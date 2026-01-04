using NFMWorld.Mad;
using NFMWorld.Util;

namespace NFMWorld.Library.backend;

public interface IRaceValues
{
    UnlimitedArray<IInGameCar> CarsInRace { get; }
    BackendStage CurrentStage { get; }
    RaceState raceState { get; }
}