using nfm_world_library.mad;
using nfm_world_library.util;

namespace nfm_world_library.backend;

public interface IRaceValues
{
    UnlimitedArray<IInGameCar> CarsInRace { get; }
    BackendStage CurrentStage { get; }
    RaceState raceState { get; }
}