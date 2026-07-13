using NFMWorldLibrary.Multiplayer.Packets.S2C;

namespace NFMWorldLibrary.Multiplayer;

/// <summary>
/// Handles chat messages and system announcements.
/// </summary>
public class ChatManager
{
    private readonly IMultiplayerServerTransport _transport;
    private readonly PlayerRegistry _players;

    public ChatManager(IMultiplayerServerTransport transport, PlayerRegistry players)
    {
        _transport = transport;
        _players = players;
    }

    /// <summary>Sends a chat message from a client to all lobby clients.</summary>
    public void SendChatMessage(uint senderClientId, string message)
    {
        var sender = _players.Get(senderClientId);
        if (sender is null) return;

        _transport.BroadcastPacket(new S2C_LobbyChatMessage
        {
            SenderClientId = senderClientId,
            Sender = sender.Name,
            Message = message
        });
    }

    /// <summary>Broadcasts a system message (e.g., join/leave announcements).</summary>
    public void BroadcastSystem(string message)
    {
        _transport.BroadcastPacket(new S2C_LobbyChatMessage
        {
            Message = message,
            Sender = "<System>",
            SenderClientId = uint.MaxValue
        });
    }
}
