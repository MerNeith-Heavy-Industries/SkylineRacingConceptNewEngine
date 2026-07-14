using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using NFMWorldLibrary.Multiplayer.HttpMessages;
using NFMWorldLibrary.Multiplayer.Packets.C2S;
using NFMWorldLibrary.Multiplayer.Packets.S2C;

namespace NFMWorldLibrary.Multiplayer;

/// <summary>
/// Game Master — dumb UDP relay for v1.
/// Validates join tokens, relays PlayerState between clients, handles race finish.
/// 
/// v2: <see cref="WorkerManager"/> and the Worker project will handle replay-based
/// validation. For now, the relay is direct.
/// </summary>
public class RaceOrchestrator : IDisposable
{
    private readonly ConcurrentDictionary<uint, ClientInfo> _clients = new();
    private readonly IMultiplayerServerTransport _transport;

    // join token → (session, player index)
    private readonly ConcurrentDictionary<Guid, (RaceSession Session, byte PlayerIndex, Guid ClientId)> _joinTokens = new();
    private readonly ConcurrentDictionary<uint, RaceSession> _sessions = new();

    public RaceOrchestrator(IMultiplayerServerTransport transport)
    {
        _transport = transport;
        transport.PacketReceived += TransportOnPacketReceived;
        transport.ClientConnected += TransportOnClientConnected;
        transport.ClientDisconnected += TransportOnClientDisconnected;
        transport.ClientConnecting += TransportOnClientConnecting;
    }

    public void Start() => _transport.Start();
    public void Stop() => _transport.Stop();

    // ── Transport events ─────────────────────────────────────────────

    private void TransportOnClientConnecting(object? sender, uint clientIndex)
        => _clients.TryAdd(clientIndex, new ClientInfo { State = ClientState.Connecting });

    private void TransportOnClientConnected(object? sender, uint clientIndex)
    {
        if (_clients.TryGetValue(clientIndex, out var c))
            c.State = ClientState.Connected;
    }

    private void TransportOnClientDisconnected(object? sender, uint clientIndex)
        => _clients.TryRemove(clientIndex, out _);

    // ── Packet dispatch ──────────────────────────────────────────────

    private void TransportOnPacketReceived(object? sender,
        (uint ClientIndex, IPacketClientToServer Packet) e)
    {
        switch (e.Packet)
        {
            case C2S_RaceLoaded raceLoaded:
                HandleRaceLoaded(e.ClientIndex, raceLoaded);
                break;
            case C2S_PlayerState playerState:
                HandlePlayerState(e.ClientIndex, playerState);
                break;
            case C2S_GameFinished gameFinished:
                HandleGameFinished(e.ClientIndex, gameFinished);
                break;
        }
    }

    // ── Packet handlers ──────────────────────────────────────────────

    private void HandleRaceLoaded(uint clientIndex, C2S_RaceLoaded raceLoaded)
    {
        if (!_joinTokens.TryGetValue(raceLoaded.JoinToken, out var entry))
        {
            Logging.Warning($"[GM] Invalid join token from client {clientIndex}");
            return;
        }

        var clientId = entry.ClientId;

        entry.Session.Clients.TryAdd(clientIndex, clientId);
        entry.Session.LoadedCount++;

        Logging.Info(
            $"[GM] Client {clientId} loaded ({entry.Session.LoadedCount}/{entry.Session.PlayerCount})");

        if (entry.Session.LoadedCount >= entry.Session.PlayerCount)
        {
            _transport.SendPacketToClients(entry.Session.Clients.Keys.ToArray(), new S2C_RaceCanStart());
            Logging.Info($"[GM] Race starting: {entry.Session.PlayerCount} players");
        }
    }

    private void HandlePlayerState(uint clientIndex, C2S_PlayerState playerState)
    {
        if (!TryFindSession(clientIndex, out var session, out var clientId)) return;

        var others = session.Clients.Keys.Where(idx => idx != clientIndex).ToArray();
        if (others.Length > 0)
        {
            _transport.SendPacketToClients(others, new S2C_PlayerState
            {
                PlayerId = clientId,
                State = playerState.State,
                CurrentServerTime = DateTimeOffset.UtcNow
            }, false);
        }
    }

    private void HandleGameFinished(uint clientIndex, C2S_GameFinished gameFinished)
    {
        if (!TryFindSession(clientIndex, out var session, out var clientId)) return;

        Logging.Info($"[GM] Client {clientId} finished in {gameFinished.RaceTime.TotalSeconds:F1}s");

        // v1: first-come first-served
        _transport.SendPacketToClients(session.Clients.Keys.ToArray(), new S2C_GameFinished
        {
            PlayerResults = new Dictionary<byte, PlayerResult>
            {
                [0] = new()
                {
                    FinishPosition = 1,
                    Finished = true,
                    RaceTime = gameFinished.RaceTime
                }
            }
        });
    }

    // ── HTTP handler ─────────────────────────────────────────────────

    public Lobby2RaceServer_CreateRaceResponse CreateRace(
        Lobby2RaceServer_CreateRace raceParams)
    {
        var sessionId = (uint)Interlocked.Increment(ref _sessionIdCounter);
        var joinTokens = new Dictionary<byte, Guid>();

        var session = new RaceSession
        {
            Id = sessionId,
            PlayerCount = raceParams.MatchGameplayInfo.Players.Count,
            Clients = []
        };

        foreach (var (playerIndex, playerInfo) in raceParams.MatchGameplayInfo.Players)
        {
            var token = Guid.NewGuid();
            joinTokens[playerIndex] = token;
            _joinTokens.TryAdd(token, (session, playerIndex, playerInfo.Id));
        }

        _sessions.TryAdd(sessionId, session);

        Logging.Info(
            $"[GM] Race created: {raceParams.MatchKey}, {joinTokens.Count} players");

        return new Lobby2RaceServer_CreateRaceResponse
        {
            PlayerSecretIds = joinTokens
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private bool TryFindSession(uint clientIndex, [NotNullWhen(true)] out RaceSession? session, out Guid clientId)
    {
        foreach (var (_, s) in _sessions)
        {
            if (s.Clients.TryGetValue(clientIndex, out var id))
            {
                session = s;
                clientId = id;
                return true;
            }
        }

        session = null;
        clientId = Guid.Empty;
        return false;
    }

    public void Dispose()
    {
        _transport.PacketReceived -= TransportOnPacketReceived;
        _transport.ClientConnected -= TransportOnClientConnected;
        _transport.ClientDisconnected -= TransportOnClientDisconnected;
        _transport.ClientConnecting -= TransportOnClientConnecting;
    }

    private class ClientInfo
    {
        public ClientState State { get; set; }
    }

    private class RaceSession
    {
        public uint Id { get; set; }
        public int PlayerCount { get; set; }
        public int LoadedCount { get; set; }
        public ConcurrentDictionary<uint, Guid> Clients { get; set; } = [];
    }

    private static int _sessionIdCounter;
}