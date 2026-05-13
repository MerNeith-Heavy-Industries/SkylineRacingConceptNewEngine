using MessagePack;

namespace NFMWorldLibrary.Multiplayer.Packets.C2S;

[MessagePackObject]
[PacketClientToServer(3)]
public partial struct C2S_LeaveSession : IPacketClientToServer<C2S_LeaveSession>
{
    [Key(0)] public required uint SessionId { get; set; }
}