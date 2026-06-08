using NFMWorldLibrary.Util;

namespace NFMWorldLibrary.Backend.Gamemodes;

public class PlayerParameters
{
    public required string PlayerName { get; init; } = "Player";
    public required string CarName { get; init; } = "nfmm/radicalone";
    public required Color3 Color { get; init; } = new Color3(255, 0, 0);
    public required bool IsBot { get; init; } = false;
    public required bool IsClientPlayer { get; init; } = false;
    // team, isbot, etc
}