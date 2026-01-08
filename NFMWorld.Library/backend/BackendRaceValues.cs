using nfm_world_library.mad;
using nfm_world_library.util;

namespace nfm_world_library.backend;

public class BackendRaceValues : IRaceValues
{
    public required UnlimitedArray<IInGameCar> CarsInRace { get; init; }
    public required BackendStage CurrentStage { get; init; }
    public required RaceState raceState { get; init; }
    
    public static BackendRaceValues Create(string stage)
    {
        var backendStage = new BackendStage(stage);
        var carsInRace = new UnlimitedArray<IInGameCar>();

        return new BackendRaceValues
        {
            CurrentStage = backendStage,
            CarsInRace = carsInRace,
            raceState = RaceState.InProgress
        };
    }

    public readonly record struct CarInit(string CarName, int X, int Z);
}