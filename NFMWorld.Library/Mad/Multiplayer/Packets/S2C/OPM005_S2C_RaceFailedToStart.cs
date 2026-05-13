using MessagePack;

namespace NFMWorldLibrary.Multiplayer.Packets.S2C;

[MessagePackObject]
[PacketServerToClient(-5)]
public partial struct S2C_RaceFailedToStart : IPacketServerToClient<S2C_RaceFailedToStart>;