using System.Collections.Concurrent;
using NFMWorldLibrary.Multiplayer.HttpMessages;
using NFMWorldLibrary.Multiplayer.Packets.C2S;
using NFMWorldLibrary.Multiplayer.Packets.S2C;

namespace NFMWorldLibrary.Multiplayer;

/// <summary>
/// Game Master orchestrator — manages race sessions, validates join tokens,
/// routes player inputs to Worker processes, and broadcasts game state to clients.
/// </summary>
public class RaceOrchestrator : IDisposable
{
    private readonly ConcurrentDictionary<uint, ClientInfo> _clients = new();
    private readonly IMultiplayerServerTransport _transport;
    private readonly WorkerManager _workerManager;

    public RaceOrchestrator(IMultiplayerServerTransport transport)
    {
        _transport = transport;
        _workerManager = new WorkerManager(transport);

        transport.PacketReceived += TransportOnPacketReceived;
        transport.ClientConnected += TransportOnClientConnected;
        transport.ClientDisconnected += TransportOnClientDisconnected;
        transport.ClientConnecting += TransportOnClientConnecting;
    }

    public void Start()
    {
        _transport.Start();
        _workerManager.StartHealthCheck();
    }

    public void Stop()
    {
        _workerManager.StopHealthCheck();
        _transport.Stop();
    }

    // ── Transport events ─────────────────────────────────────────────

    private void TransportOnClientConnecting(object? sender, uint clientIndex)
    {
        _clients.TryAdd(clientIndex, new ClientInfo { State = ClientState.Connecting });
    }

    private void TransportOnClientConnected(object? sender, uint clientIndex)
    {
        if (_clients.TryGetValue(clientIndex, out var client))
            client.State = ClientState.Connected;
    }

    private void TransportOnClientDisconnected(object? sender, uint clientIndex)
    {
        _clients.TryRemove(clientIndex, out _);
    }

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

    private void HandleRaceLoaded(uint clientId, C2S_RaceLoaded raceLoaded)
    {
        var session = _workerManager.ValidateJoinToken(clientId, raceLoaded.JoinToken);
        if (session is null)
        {
            Console.WriteLine(
                $"[RaceOrchestrator] Invalid join token from client {clientId}");
            return;
        }

        Console.WriteLine(
            $"[RaceOrchestrator] Client {clientId} loaded for match {session.MatchKey}");

        // Check if all players have loaded
        var allLoaded = session.JoinTokens.Values.All(e => e.ClientId != 0);

        if (allLoaded)
        {
            var clientIds = session.JoinTokens.Values
                .Select(e => e.ClientId)
                .ToArray();

            _transport.SendPacketToClients(clientIds, new S2C_RaceCanStart());
            Console.WriteLine(
                $"[RaceOrchestrator] Race can start: {session.MatchKey}");
        }
    }

    private void HandlePlayerState(uint clientId, C2S_PlayerState playerState)
    {
        _workerManager.ForwardPlayerInput(clientId, playerState.State);
    }

    private void HandleGameFinished(uint clientId, C2S_GameFinished gameFinished)
    {
        var session = _workerManager.GetSessionForClient(clientId);
        if (session is null) return;

        Console.WriteLine(
            $"[RaceOrchestrator] Client {clientId} reports race finished: " +
            $"{gameFinished.RaceTime.TotalSeconds:F1}s");
    }

    // ── HTTP handler (called from Program.cs) ────────────────────────

    public Lobby2RaceServer_CreateRaceResponse CreateRace(
        Lobby2RaceServer_CreateRace raceParams)
    {
        var sessionId = (uint)Interlocked.Increment(ref _sessionIdCounter);
        var joinTokens = new Dictionary<byte, Guid>();

        foreach (var (playerIndex, _) in raceParams.MatchGameplayInfo.Players)
            joinTokens[playerIndex] = Guid.NewGuid();

        _workerManager.CreateWorker(
            sessionId,
            raceParams.MatchKey,
            raceParams.MatchGameplayInfo,
            joinTokens);

        Console.WriteLine(
            $"[RaceOrchestrator] Race created: {raceParams.MatchKey}, " +
            $"{joinTokens.Count} players");

        return new Lobby2RaceServer_CreateRaceResponse
        {
            PlayerSecretIds = joinTokens
        };
    }

    public void Dispose()
    {
        _transport.PacketReceived -= TransportOnPacketReceived;
        _transport.ClientConnected -= TransportOnClientConnected;
        _transport.ClientDisconnected -= TransportOnClientDisconnected;
        _transport.ClientConnecting -= TransportOnClientConnecting;
        _workerManager.Dispose();
    }

    private class ClientInfo
    {
        public ClientState State { get; set; }
    }

    private static int _sessionIdCounter;
}