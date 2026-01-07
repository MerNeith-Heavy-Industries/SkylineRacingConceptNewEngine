using nfm_world_library.mad;
using nfm_world_library.util;

namespace nfm_world_library.backend;

public class BackendRaceValues : IRaceValues
{
    public required UnlimitedArray<IInGameCar> CarsInRace { get; init; }
    public required BackendStage CurrentStage { get; init; }
    public required RaceState raceState { get; init; }
    
    public static BackendRaceValues Create(string stage, CarInit[] cars)
    {
        var backendStage = new BackendStage(stage);
        var carsInRace = new UnlimitedArray<IInGameCar>();
        for (int i = 0; i < cars.Length; i++)
        {
            var backendCar = new BackendCar(BackendGameSparker.GetCar(cars[i].CarName).Rad!, i, cars[i].X, cars[i].Z, true);
            carsInRace.Add(backendCar);
        }

        return new BackendRaceValues
        {
            CurrentStage = backendStage,
            CarsInRace = carsInRace,
            raceState = RaceState.InProgress
        };
    }

    public readonly record struct CarInit(string CarName, int X, int Z);
}