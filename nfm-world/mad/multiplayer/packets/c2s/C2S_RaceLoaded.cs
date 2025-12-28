using MessagePack;

namespace NFMWorld.Mad;

[MessagePackObject]
public struct C2S_RaceLoaded : IPacketClientToServer<C2S_RaceLoaded>;