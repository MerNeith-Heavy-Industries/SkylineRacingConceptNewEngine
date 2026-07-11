using MemoryPack;
using NFMWorldLibrary.Multiplayer.Packets.S2C;

namespace NFMWorldLibrary.Multiplayer.HttpMessages;

[MemoryPackable]
public partial struct Lobby2RaceServer_CreateRaceResponse
{
    /// <summary>
    /// Key: player car index as in <see cref="MatchGameplayInfo"/>
    /// Value: Secret GUID that said player can use to authenticate with the race server.
    /// </summary>
    [MemoryPackOrder(0)] public required IDictionary<byte, Guid> PlayerSecretIds { get; set; }
}