using MessagePack;

namespace NFMWorld.Mad;

[MessagePackObject]
public struct C2S_LeaveSession : IPacketClientToServer<C2S_LeaveSession>
{
    [Key(0)] public required uint SessionId { get; set; }
}