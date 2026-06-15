using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.Util;
using WorldXaml.UI.Yoga;

namespace NFMWorldLibrary.Backend;

/// <summary>
/// Data for the gamemode.
/// </summary>
public interface IGamemodeData
{
    ObservableUnlimitedArray<IInGameCar> CarsInRace { get; }
    BackendStage CurrentStage { get; }
    RaceState raceState { get; }

    [ClientOnly]
    IClientCallbacks ClientCallbacks { get; }
    
    [ClientOnly]
    FocusManager FocusManager { get; }
}