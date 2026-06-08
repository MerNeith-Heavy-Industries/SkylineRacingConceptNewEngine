using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.Util;

namespace NFMWorldLibrary.Backend;

/// <summary>
/// Data for the gamemode.
/// </summary>
public interface IGamemodeData
{
    UnlimitedArray<IInGameCar> CarsInRace { get; }
    BackendStage CurrentStage { get; }
    RaceState raceState { get; }

    [ClientOnly]
    IClientCallbacks ClientCallbacks { get; }
}