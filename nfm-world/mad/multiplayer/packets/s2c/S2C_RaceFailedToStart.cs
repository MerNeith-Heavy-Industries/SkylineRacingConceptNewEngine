using MessagePack;

namespace NFMWorld.Mad;

[MessagePackObject]
public struct S2C_RaceFailedToStart : IPacketServerToClient<S2C_RaceFailedToStart>;