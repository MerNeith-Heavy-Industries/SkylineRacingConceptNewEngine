using MemoryPack;

namespace NFMWorldLibrary.Multiplayer.Packets.C2S;

/// <summary>
/// Client → Game Master: reports that the local player has finished the race.
/// v1: first-come first-served, full trust (no replay validation).
/// </summary>
[MemoryPackable]
[PacketClientToServer(10)]
public partial struct C2S_GameFinished : IPacketClientToServer<C2S_GameFinished>
{
    /// <summary>Join token for this race session.</summary>
    [MemoryPackOrder(0)] public required Guid JoinToken { get; set; }

    /// <summary>Local race time at finish.</summary>
    [MemoryPackOrder(1)] public required TimeSpan RaceTime { get; set; }
}
