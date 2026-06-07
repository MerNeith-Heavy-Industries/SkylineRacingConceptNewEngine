using System.Runtime.InteropServices;
using MemoryPack;

namespace NFMWorldLibrary.Multiplayer.Packets.S2C;

[MemoryPackable]
[PacketServerToClient(-6)]
public partial struct S2C_RaceStarted : IPacketServerToClient<S2C_RaceStarted>
{
    [StructLayout(LayoutKind.Sequential)]
    [MemoryPackable]
    public partial struct GameSession
    {
        [MemoryPackOrder(0)] public required string StageName { get; set; }
        
        /// <summary>
        /// Key: player car index
        /// Value: client ID
        /// </summary>
        [MemoryPackOrder(1)] public required IDictionary<byte, PlayerInfo> Players { get; set; }
        [MemoryPackOrder(2)] public required SessionState State { get; set; } = SessionState.NotStarted;
        [MemoryPackOrder(3)] public required GameModes Gamemode { get; set; } = GameModes.Sandbox;
        
        public GameSession()
        {
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    [MemoryPackable]
    public partial struct PlayerInfo
    {
        [MemoryPackOrder(0)] public required uint Id { get; set; }
        [MemoryPackOrder(1)] public required string Name { get; set; }
        [MemoryPackOrder(2)] public required string Vehicle { get; set; }
        [MemoryPackOrder(3)] public required Color3 Color { get; set; }
    }
    
    [MemoryPackOrder(0)] public required GameSession Session { get; set; }
}