using System.Collections.Concurrent;
using NFMWorldLibrary.Multiplayer.HttpMessages;
using NFMWorldLibrary.Multiplayer.Packets.C2S;

namespace NFMWorldLibrary.Multiplayer;

public class RaceOrchestrator
{
    private ConcurrentDictionary<uint, ClientInfo> _connectedClients = new();
    private Thread _lobbyThread;
    private bool _lobbyIsRunning = true;
    private readonly IMultiplayerServerTransport _transport;
    
    private ConcurrentDictionary<uint, GameSession> _activeSessions = new();

    private uint _maxSessionId = 0;

    public RaceOrchestrator(IMultiplayerServerTransport transport)
    {
        _transport = transport;
        transport.PacketReceived += TransportOnPacketReceived;
        transport.ClientConnected += TransportOnClientConnected;
        transport.ClientDisconnected += TransportOnClientDisconnected;
        transport.ClientConnecting += TransportOnClientConnecting;
    }

    public void Start()
    {
        _transport.Start();
    }

    public void Stop()
    {
        _transport.Stop();
    }

    private void TransportOnClientConnecting(object? sender, uint clientIndex)
    {
        _connectedClients.TryAdd(clientIndex, new ClientInfo()
        {
            State = ClientState.Connecting
        });
    }

    private void TransportOnClientDisconnected(object? sender, uint clientIndex)
    {
        if (_connectedClients.TryRemove(clientIndex, out var client))
        {
            if (client.InSession is {} inSession && _activeSessions.TryGetValue(inSession.SessionIndex, out var session))
            {
                session.PlayerClientIds.TryRemove(KeyValuePair.Create(inSession.PlayerIndex, clientIndex));
            }
        }
    }

    private void TransportOnClientConnected(object? sender, uint clientIndex)
    {
        if (_connectedClients.TryGetValue(clientIndex, out var clientInfo))
        {
            clientInfo.State = ClientState.Connected;
        }
    }

    private void TransportOnPacketReceived(object? sender, (uint ClientIndex, IPacketClientToServer Packet) e)
    {
        switch (e.Packet)
        {
        }
    }

    private class GameSession
    {
        public ConcurrentDictionary<byte, uint> PlayerClientIds { get; set; } = [];
        public DateTimeOffset? StartTime { get; set; }
        
        public required MatchGameplayInfo GameplayInfo { get; set; }
        public required string MatchKey { get; set; }
    }

    private class ClientInfo
    {
        public ClientState State { get; set; }
        public (byte PlayerIndex, uint SessionIndex)? InSession { get; set; }
    }

    public Lobby2RaceServer_CreateRaceResponse CreateRace(Lobby2RaceServer_CreateRace raceParams)
    {
        var sessionId = Interlocked.Increment(ref _maxSessionId);
        var session = new GameSession
        {
            GameplayInfo = raceParams.MatchGameplayInfo,
            MatchKey = raceParams.MatchKey
        };
        
        var joinTokens = raceParams.MatchGameplayInfo
    }
}