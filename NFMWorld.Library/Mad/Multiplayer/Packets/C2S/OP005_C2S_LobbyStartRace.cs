using MessagePack;

namespace NFMWorldLibrary.Multiplayer.Packets.C2S;

[MessagePackObject]
[PacketClientToServer(5)]
public partial struct C2S_LobbyStartRace : IPacketClientToServer<C2S_LobbyStartRace>
{
    [Key(0)] public required uint SessionId { get; set; }
    
    public C2S_LobbyStartRace()
    {
    }
}