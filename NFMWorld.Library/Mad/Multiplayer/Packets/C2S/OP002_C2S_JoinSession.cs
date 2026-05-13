using MessagePack;

namespace NFMWorldLibrary.Multiplayer.Packets.C2S;

[MessagePackObject]
[PacketClientToServer(2)]
public partial struct C2S_JoinSession : IPacketClientToServer<C2S_JoinSession>
{
    [Key(0)] public required uint SessionId { get; set; }
}