using MemoryPack;
using NFMWorld.Server.SharedMemory;
using NFMWorldLibrary;
using NFMWorldLibrary.Multiplayer;

// ── Parse CLI arguments ──────────────────────────────────────────

BackendGameSparker.Load();

var shmName = GetArg(args, "--shm-name");
var stageName = GetArg(args, "--stage") ?? "unknown";
var gamemode = int.TryParse(GetArg(args, "--gamemode"), out var gm)
    ? (GameModes)gm : GameModes.Sandbox;
var playersB64 = GetArg(args, "--players");

if (string.IsNullOrEmpty(shmName))
{
    Console.Error.WriteLine("[Worker] FATAL: --shm-name is required");
    return 1;
}

// ── Parse player config ───────────────────────────────────────────

Dictionary<byte, PlayerInfo> players;
if (!string.IsNullOrEmpty(playersB64))
{
    var playerData = MemoryPackSerializer.Deserialize<MatchGameplayInfo>(
        Convert.FromBase64String(playersB64));
    players = playerData.Players?.ToDictionary(kv => kv.Key, kv => kv.Value)
              ?? new Dictionary<byte, PlayerInfo>();
}
else
{
    players = new Dictionary<byte, PlayerInfo>();
}

Console.WriteLine(
    $"[Worker] Starting: shm={shmName}, stage={stageName}, " +
    $"gamemode={gamemode}, players={players.Count}");

// ── Create simulation ─────────────────────────────────────────────

var raceLoop = new RaceWorker(stageName, gamemode, players);
var shutdownTcs = new TaskCompletionSource();

// ── Open RPC channel (Worker side) ─────────────────────────────────

// The Worker opens the RPC channel AFTER the Controller.
// RpcBuffer auto-detects: first constructor = Controller, second = Worker.
// The Controller creates the shared memory; the Worker opens the existing buffers.

// ── RPC handler: just accumulate inputs, don't tick ──────────────
// The main 63 TPS loop drives the simulation.

uint _tickCounter = 0;

RpcMessage HandleRequest(RpcMessage request)
{
    switch (request.Type)
    {
        case RpcMessageType.PlayerInputs:
        {
            // Accumulate inputs for the next tick — don't process yet
            var batch = request.Deserialize<PlayerInputBatch>();
            foreach (var (playerIndex, state) in batch.PlayerStates)
                raceLoop.AccumulateInput(playerIndex, state);
            return new RpcMessage { Type = RpcMessageType.PlayerInputs };
        }

        case RpcMessageType.Shutdown:
        {
            Console.WriteLine("[Worker] Shutdown requested");
            raceLoop.Stop();
            shutdownTcs.TrySetResult();
            return new RpcMessage { Type = RpcMessageType.Shutdown };
        }

        default:
        {
            Console.Error.WriteLine($"[Worker] Unknown RPC type: {request.Type}");
            return new RpcMessage { Type = RpcMessageType.Error };
        }
    }
}

using var rpcBridge = new RpcBridge(shmName, HandleRequest);

// Start the simulation (triggers countdown)
raceLoop.Start();

Console.WriteLine("[Worker] Starting 63 TPS simulation loop...");

// ── 63 TPS tick loop ──────────────────────────────────────────────

var tickInterval = TimeSpan.FromMilliseconds(1000.0 / 63.0);
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
long lastTick = 0;

while (!shutdownTcs.Task.IsCompleted && !raceLoop.RaceFinishedTask.IsCompleted)
{
    var elapsed = stopwatch.ElapsedTicks;
    var targetTick = elapsed / tickInterval.Ticks;

    while (lastTick < targetTick && !raceLoop.RaceFinishedTask.IsCompleted)
    {
        // Snap accumulated inputs and tick simulation
        var inputs = raceLoop.SnapInputs(_tickCounter++);
        var state = raceLoop.Tick(inputs);

        // Send state to Controller
        try
        {
            var stateMsg = RpcMessage.Create(RpcMessageType.GameState, state);
            rpcBridge.Send(stateMsg, timeoutMs: 10);
        }
        catch (TimeoutException)
        {
            // Controller busy; state will be sent next tick
        }

        lastTick++;
    }

    Thread.Sleep(1);
}

// ── Race complete ─────────────────────────────────────────────────

if (raceLoop.RaceFinishedTask.IsCompleted)
{
    Console.WriteLine("[Worker] Simulation reported race complete");
    var results = raceLoop.GetFinalResults();
    Console.WriteLine($"[Worker] Results: {results.Count} players");
}

shutdownTcs.TrySetResult();

// ── Wait for shutdown ─────────────────────────────────────────────

await shutdownTcs.Task;

Console.WriteLine($"[Worker] Exiting");
return 0;

// ── Helpers ───────────────────────────────────────────────────────

static string? GetArg(string[] args, string name)
{
    var idx = Array.IndexOf(args, name);
    return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
}