using MessagePack;

namespace NFMWorldLibrary.Multiplayer.Packets.C2S;

[MessagePackObject]
[PacketClientToServer(8)]
public partial struct C2S_RaceLoaded : IPacketClientToServer<C2S_RaceLoaded>;