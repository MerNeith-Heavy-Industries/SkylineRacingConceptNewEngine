using MessagePack;

namespace nfm_world.multiplayer.packets.s2c;

[MessagePackObject]
public struct S2C_RaceFailedToStart : IPacketServerToClient<S2C_RaceFailedToStart>;