using MessagePack;

namespace nfm_world.multiplayer.packets.c2s;

[MessagePackObject]
public struct C2S_LobbyStartRace : IPacketClientToServer<C2S_LobbyStartRace>
{
    [Key(0)] public required uint SessionId { get; set; }
    
    public C2S_LobbyStartRace()
    {
    }
}