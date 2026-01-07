using MessagePack;

namespace nfm_world.multiplayer.packets.c2s;

[MessagePackObject]
public struct C2S_RaceLoaded : IPacketClientToServer<C2S_RaceLoaded>;