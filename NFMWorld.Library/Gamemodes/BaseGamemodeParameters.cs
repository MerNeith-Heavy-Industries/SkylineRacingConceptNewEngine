namespace NFMWorldLibrary.Backend.Gamemodes;

public class BaseGamemodeParameters
{
    public required IReadOnlyList<PlayerParameters> Players { get; init; }
}