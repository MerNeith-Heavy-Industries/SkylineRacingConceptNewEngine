using NFMWorldLibrary.Multiplayer.Packets.S2C;

namespace NFMWorldLibrary.Multiplayer;

/// <summary>
/// Builds and broadcasts <see cref="S2C_LobbyState"/> snapshots to all connected clients.
/// </summary>
public class LobbyStateBroadcaster
{
    private readonly IMultiplayerServerTransport _transport;
    private readonly PlayerRegistry _players;
    private readonly SessionManager _sessions;

    public LobbyStateBroadcaster(
        IMultiplayerServerTransport transport,
        PlayerRegistry players,
        SessionManager sessions)
    {
        _transport = transport;
        _players = players;
        _sessions = sessions;
    }

    /// <summary>Broadcasts a full lobby state snapshot to every connected client.</summary>
    public void BroadcastToAll()
    {
        foreach (var (clientId, _) in _players.All)
        {
            var packet = BuildSnapshot(clientId);
            _transport.SendPacketToClient(clientId, packet);
        }
    }

    /// <summary>Builds a <see cref="S2C_LobbyState"/> for a specific client.</summary>
    public S2C_LobbyState BuildSnapshot(uint playerClientId)
    {
        var playerList = new List<PlayerInfo>();
        var sessionList = new List<S2C_LobbyState.GameSession>();

        foreach (var (id, client) in _players.All)
        {
            playerList.Add(new PlayerInfo
            {
                Id = id,
                Name = client.Name,
                Vehicle = client.Vehicle,
                Color = client.Color
            });
        }

        foreach (var (_, session) in _sessions.All)
        {
            sessionList.Add(new S2C_LobbyState.GameSession
            {
                Id = session.Id,
                CreatorId = session.CreatorId,
                CreatorName = session.CreatorName,
                StageName = session.StageName,
                MaxPlayers = session.MaxPlayers,
                PlayerClientIds = session.PlayerClientIds,
                State = session.State
            });
        }

        return new S2C_LobbyState
        {
            PlayerClientId = playerClientId,
            Players = playerList,
            ActiveSessions = sessionList
        };
    }
}
