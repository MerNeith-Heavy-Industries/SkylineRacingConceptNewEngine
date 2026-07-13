using System.Collections.Concurrent;
using System.Diagnostics;
using MemoryPack;
using NFMWorld.Server.SharedMemory;
using NFMWorldLibrary.Multiplayer.HttpMessages;
using NFMWorldLibrary.Multiplayer.Packets.S2C;

namespace NFMWorldLibrary.Multiplayer;

/// <summary>
/// Manages Worker processes: spawn, forward player inputs immediately via RPC,
/// broadcast game state received from Workers, health monitoring, and cleanup.
/// 
/// The Worker owns its own 63 TPS simulation loop. This manager just relays
/// inputs to the right Worker and broadcasts state back to clients.
/// </summary>
public class WorkerManager(IMultiplayerServerTransport transport) : IDisposable
{
    private readonly ConcurrentDictionary<uint, WorkerProcess> _sessions = new();

    private readonly string _workerBinaryPath = Environment.GetEnvironmentVariable("WORKER_BINARY_PATH")
                                                ?? "dotnet";
    private Thread? _healthThread;
    private bool _running;

    public WorkerProcess CreateWorker(
        uint sessionId, string matchKey,
        MatchGameplayInfo gameplayInfo, IDictionary<byte, Guid> joinTokens)
    {
        var shmName = $"nfmw-race-{Guid.NewGuid():N}";

        var playerConfigJson = MemoryPackSerializer.Serialize(gameplayInfo);
        var playerConfigB64 = Convert.ToBase64String(playerConfigJson);

        var args = new List<string>();
        if (_workerBinaryPath == "dotnet")
        {
            var workerProjectPath = Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..",
                "..", "NFMWorld.Server.Game.Worker",
                "NFMWorld.Server.Game.Worker.csproj");
            args.Add("run"); args.Add("--project");
            args.Add(Path.GetFullPath(workerProjectPath)); args.Add("--");
        }
        else { args.Add(_workerBinaryPath); }

        args.Add("--shm-name"); args.Add(shmName);
        args.Add("--stage"); args.Add(gameplayInfo.StageName ?? "");
        args.Add("--gamemode"); args.Add(((int)gameplayInfo.Gamemode).ToString());
        args.Add("--players"); args.Add(playerConfigB64);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _workerBinaryPath == "dotnet" ? "dotnet" : _workerBinaryPath,
                Arguments = _workerBinaryPath == "dotnet"
                    ? string.Join(" ", args)
                    : string.Join(" ", args.Skip(1)),
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                Console.WriteLine($"[Worker:{matchKey[..6]}] {e.Data}");
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                Console.Error.WriteLine($"[Worker:{matchKey[..6]}] ERR: {e.Data}");
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var rpcBridge = new RpcBridge(shmName, HandleWorkerMessage,
            bufferCapacity: 100000, bufferNodeCount: 10);

        var worker = new WorkerProcess
        {
            SessionId = sessionId, MatchKey = matchKey,
            Process = process, RpcBridge = rpcBridge,
            JoinTokens = new Dictionary<Guid, (byte PlayerIndex, uint ClientId)>()
        };

        foreach (var (playerIndex, token) in joinTokens)
            worker.JoinTokens[token] = (playerIndex, 0);

        _sessions.TryAdd(sessionId, worker);
        return worker;
    }

    public void StartHealthCheck(int intervalMs = 500)
    {
        _running = true;
        _healthThread = new Thread(() => HealthLoop(intervalMs))
        {
            IsBackground = true,
            Name = "WorkerManager-Health"
        };
        _healthThread.Start();
    }

    public void StopHealthCheck()
    {
        _running = false;
        _healthThread?.Join(TimeSpan.FromSeconds(5));
    }

    public WorkerProcess? GetSessionForClient(uint clientId)
    {
        foreach (var (_, s) in _sessions)
            foreach (var (_, (_, cid)) in s.JoinTokens)
                if (cid == clientId) return s;
        return null;
    }

    public WorkerProcess? ValidateJoinToken(uint clientId, Guid joinToken)
    {
        foreach (var (_, s) in _sessions)
        {
            if (s.JoinTokens.TryGetValue(joinToken, out var entry))
            { s.JoinTokens[joinToken] = (entry.PlayerIndex, clientId); return s; }
        }
        return null;
    }

    /// <summary>
    /// Forwards a player input to the Worker immediately via RPC (fire-and-forget).
    /// The Worker accumulates inputs and processes them on its own 63 TPS loop.
    /// </summary>
    public void ForwardPlayerInput(uint clientId, PlayerState state)
    {
        var session = GetSessionForClient(clientId);
        if (session is null) return;

        byte? playerIndex = null;
        foreach (var (_, (idx, cid)) in session.JoinTokens)
            if (cid == clientId) { playerIndex = idx; break; }
        if (playerIndex is null) return;

        var batch = new PlayerInputBatch
        {
            TickNumber = 0,
            ServerTime = DateTimeOffset.UtcNow,
            PlayerStates = new Dictionary<byte, PlayerState>
            { [playerIndex.Value] = state }
        };

        try
        {
            var msg = RpcMessage.Create(RpcMessageType.PlayerInputs, batch);
            session.RpcBridge.Send(msg, timeoutMs: 5);
        }
        catch { /* Worker busy; input will be stale next frame anyway */ }
    }

    public void TerminateWorker(uint sessionId)
    {
        if (!_sessions.TryRemove(sessionId, out var session)) return;

        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            session.RpcBridge.Send(
                new RpcMessage { Type = RpcMessageType.Shutdown },
                timeoutMs: 3000,
                cancellationToken: cts.Token
            );
        }
        catch { }

        session.RpcBridge.Dispose();

        if (!session.Process.HasExited)
        { session.Process.Kill(true); session.Process.WaitForExit(5000); }

        session.Process.Dispose();
        Console.WriteLine($"[WorkerManager] Worker terminated: {session.MatchKey}");
    }

    // ── Health loop ──────────────────────────────────────────────────

    private void HealthLoop(int intervalMs)
    {
        while (_running)
        {
            foreach (var (sid, session) in _sessions)
            {
                if (session.Process.HasExited)
                {
                    Console.WriteLine($"[WorkerManager] Worker exited: {session.MatchKey}");
                    TerminateWorker(sid);
                }
            }
            Thread.Sleep(intervalMs);
        }
    }

    // ── State broadcasting ───────────────────────────────────────────

    /// <summary>Called when the Worker sends a GameState RPC. Broadcasts to clients.</summary>
    public void OnGameStateReceived(WorkerProcess session, GameStateSnapshot snapshot)
    {
        foreach (var (playerIndex, state) in snapshot.PlayerStates)
        {
            uint? targetClientId = null;
            foreach (var (_, (idx, cid)) in session.JoinTokens)
            {
                if (idx == playerIndex)
                {
                    targetClientId = cid;
                    break;
                }
            }

            if (targetClientId is null) continue;

            var others = session.JoinTokens.Values
                .Where(e => e.ClientId != targetClientId.Value)
                .Select(e => e.ClientId).ToArray();

            if (others.Length > 0)
            {
                transport.SendPacketToClients(others, new S2C_PlayerState
                {
                    PlayerClientId = targetClientId.Value,
                    State = state,
                    CurrentServerTime = snapshot.ServerTime
                }, false);
            }
        }

        if (snapshot.IsRaceFinished)
            HandleRaceComplete(session);
    }

    private void HandleRaceComplete(WorkerProcess session)
    {
        Console.WriteLine($"[WorkerManager] Race complete: {session.MatchKey}");

        var clientIds = session.JoinTokens.Values
            .Select(e => e.ClientId)
            .ToArray();

        if (clientIds.Length > 0)
        {
            transport.SendPacketToClients(
                clientIds,
                new S2C_GameFinished
                {
                    PlayerResults = new Dictionary<byte, PlayerResult>()
                }
            );
        }

        TerminateWorker(session.SessionId);
    }

    /// <summary>
    /// Handles RPC messages initiated by the Worker (game state updates).
    /// </summary>
    private RpcMessage HandleWorkerMessage(RpcMessage message)
    {
        if (message.Type == RpcMessageType.GameState)
        {
            var state = message.Deserialize<GameStateSnapshot>();
            foreach (var (_, s) in _sessions)
            {
                if (s.RpcBridge != null)
                {
                    OnGameStateReceived(s, state);
                    break;
                } // TODO: map bridge→session
            }
        }
        return new RpcMessage { Type = RpcMessageType.GameState };
    }

    public void Dispose()
    {
        _running = false;
        _healthThread?.Join(TimeSpan.FromSeconds(5));
        foreach (var (sid, _) in _sessions) TerminateWorker(sid);
    }

    public class WorkerProcess
    {
        public required uint SessionId { get; init; }
        public required string MatchKey { get; init; }
        public required Process Process { get; init; }
        public required RpcBridge RpcBridge { get; init; }
        public required Dictionary<Guid, (byte PlayerIndex, uint ClientId)> JoinTokens { get; init; }
    }
}
