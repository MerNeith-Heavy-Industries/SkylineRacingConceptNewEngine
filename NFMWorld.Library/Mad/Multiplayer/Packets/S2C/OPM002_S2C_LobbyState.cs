using System.Runtime.InteropServices;
using MemoryPack;

namespace NFMWorldLibrary.Multiplayer.Packets.S2C;

[MemoryPackable]
[PacketServerToClient(-2)]
public partial struct S2C_LobbyState : IPacketServerToClient<S2C_LobbyState>
{
    [MemoryPackOrder(0)] public required uint PlayerClientId { get; set; }
    [MemoryPackOrder(1)] public required IList<PlayerInfo> Players { get; set; }
    [MemoryPackOrder(2)] public required IList<GameSession> ActiveSessions { get; set; }
    
    [StructLayout(LayoutKind.Sequential)]
    [MemoryPackable]
    public partial struct PlayerInfo
    {
        [MemoryPackOrder(0)] public required uint Id { get; set; }
        [MemoryPackOrder(1)] public required string Name { get; set; }
        [MemoryPackOrder(2)] public required string Vehicle { get; set; }
        [MemoryPackOrder(3)] public Color3 Color { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    [MemoryPackable]
    public partial struct GameSession
    {
        [MemoryPackOrder(0)] public required uint Id { get; set; }
        [MemoryPackOrder(1)] public required uint CreatorId { get; set; }
        [MemoryPackOrder(2)] public required string CreatorName { get; set; }
        [MemoryPackOrder(3)] public required string StageName { get; set; }
        [MemoryPackOrder(6)] public required int MaxPlayers { get; set; }
        
        /// <summary>
        /// Key: player car index
        /// Value: client ID
        /// </summary>
        [MemoryPackOrder(4)] public required IDictionary<byte, uint> PlayerClientIds { get; set; }
        [MemoryPackOrder(5)] public required SessionState State { get; set; } = SessionState.NotStarted;

        [MemoryPackIgnore] public int PlayerCount => PlayerClientIds.Count;

        public GameSession()
        {
        }
    }
}