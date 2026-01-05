using MessagePack;

namespace nfm_world.multiplayer.packets.c2s;

[MessagePackObject]
public struct C2S_PlayerState : IPacketClientToServer<C2S_PlayerState>
{
    [Key(0)] public required PlayerState State;

    public C2S_PlayerState()
    {
    }
}