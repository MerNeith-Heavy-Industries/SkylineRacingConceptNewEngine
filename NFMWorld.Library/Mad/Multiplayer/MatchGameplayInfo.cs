using System.Runtime.InteropServices;
using MemoryPack;

namespace NFMWorldLibrary.Multiplayer;

/// <summary>
/// Required gameplay info to construct a match on the client or server.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[MemoryPackable]
public partial struct MatchGameplayInfo()
{
    [MemoryPackOrder(0)] public required string StageName { get; set; }
        
    /// <summary>
    /// Key: player car index
    /// Value: client ID
    /// </summary>
    [MemoryPackOrder(1)] public required IDictionary<byte, PlayerInfo> Players { get; set; }

    [MemoryPackOrder(2)] public required GameModes Gamemode { get; set; } = GameModes.Sandbox;
}