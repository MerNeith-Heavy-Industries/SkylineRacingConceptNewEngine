using MessagePack;

namespace NFMWorldLibrary.Multiplayer.Packets.C2S;

[MessagePackObject]
[PacketClientToServer(1)]
public partial struct C2S_CreateSession : IPacketClientToServer<C2S_CreateSession>
{
    [Key(0)] public required string StageName { get; set; }
    [Key(1)] public required int MaxPlayers { get; set; }
    [Key(2)] public required GameModes GameMode { get; set; }
}
