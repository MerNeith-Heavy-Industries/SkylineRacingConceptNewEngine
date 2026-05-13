using MessagePack;

namespace NFMWorldLibrary.Multiplayer.Packets.C2S;

[MessagePackObject]
[PacketClientToServer(7)]
public partial struct C2S_PlayerState : IPacketClientToServer<C2S_PlayerState>
{
    [Key(0)] public required PlayerState State;

    public C2S_PlayerState()
    {
    }
}