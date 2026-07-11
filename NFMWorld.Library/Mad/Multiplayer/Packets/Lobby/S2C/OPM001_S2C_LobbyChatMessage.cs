using MemoryPack;

namespace NFMWorldLibrary.Multiplayer.Packets.S2C;

[MemoryPackable]
[PacketServerToClient(-1)]
public partial struct S2C_LobbyChatMessage : IPacketServerToClient<S2C_LobbyChatMessage>
{
    [MemoryPackOrder(0)] public required string Sender { get; set; } = string.Empty;
    [MemoryPackOrder(1)] public required uint SenderClientId { get; set; } = 0;
    [MemoryPackOrder(2)] public required string Message { get; set; } = string.Empty;

    public S2C_LobbyChatMessage()
    {
    }
}