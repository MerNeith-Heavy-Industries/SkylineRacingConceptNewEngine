using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.Util;

namespace NFMWorldLibrary.Backend;

public class BackendGamemodeData : IGamemodeData
{
    public required UnlimitedArray<IInGameCar> CarsInRace { get; init; }
    public required BackendStage CurrentStage { get; init; }
    public required RaceState raceState { get; init; }
    public IClientCallbacks ClientCallbacks => ClientServer.AccidentallyCalledClientMethodOnServer<IClientCallbacks>();

    public static BackendGamemodeData Create(string stage)
    {
        var backendStage = new BackendStage(stage);
        var carsInRace = new UnlimitedArray<IInGameCar>();

        return new BackendGamemodeData
        {
            CurrentStage = backendStage,
            CarsInRace = carsInRace,
            raceState = RaceState.InProgress
        };
    }

    public static IGamemodeData Create(string stage, StageLoader stageData)
    {
        var backendStage = new BackendStage(stage, stageData);
        var carsInRace = new UnlimitedArray<IInGameCar>();

        return new BackendGamemodeData
        {
            CurrentStage = backendStage,
            CarsInRace = carsInRace,
            raceState = RaceState.InProgress
        };
    }
}
