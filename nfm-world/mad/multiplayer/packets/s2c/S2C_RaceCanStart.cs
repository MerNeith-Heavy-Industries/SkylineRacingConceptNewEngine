using MessagePack;

namespace NFMWorld.Mad;

[MessagePackObject]
public struct S2C_RaceCanStart : IPacketServerToClient<S2C_RaceCanStart>;