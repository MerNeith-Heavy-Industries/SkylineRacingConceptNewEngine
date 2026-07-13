using MemoryPack;

namespace NFMWorldLibrary.Multiplayer.Packets.S2C;

/// <summary>
/// Game Master → Client: the race has ended. Contains final results.
/// </summary>
[MemoryPackable]
[PacketServerToClient(-7)]
public partial struct S2C_GameFinished : IPacketServerToClient<S2C_GameFinished>
{
    /// <summary>Player finish results, keyed by player index.</summary>
    [MemoryPackOrder(0)] public required Dictionary<byte, PlayerResult> PlayerResults { get; set; }
}

/// <summary>
/// Result for a single player at race end (mirrors RPC version in Server.SharedMemory).
/// </summary>
[MemoryPackable]
public partial struct PlayerResult
{
    /// <summary>Finish position (1-based). 0 = DNF.</summary>
    [MemoryPackOrder(0)] public byte FinishPosition { get; set; }

    /// <summary>Total race time for this player.</summary>
    [MemoryPackOrder(1)] public TimeSpan RaceTime { get; set; }

    /// <summary>Whether the player completed the race (false = DNF/disconnect).</summary>
    [MemoryPackOrder(2)] public bool Finished { get; set; }
}
