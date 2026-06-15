using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.Util;
using WorldXaml.UI.Yoga;

namespace NFMWorldLibrary.Backend;

public class BackendGamemodeData : IGamemodeData
{
    public required ObservableUnlimitedArray<IInGameCar> CarsInRace { get; init; }
    public required BackendStage CurrentStage { get; init; }
    public required RaceState raceState { get; init; }
    public IClientCallbacks ClientCallbacks => ClientServer.AccidentallyCalledClientMethodOnServer<IClientCallbacks>();
    public FocusManager FocusManager => ClientServer.AccidentallyCalledClientMethodOnServer<FocusManager>();

    public static BackendGamemodeData Create(string stage)
    {
        var backendStage = new BackendStage(stage);
        var carsInRace = new ObservableUnlimitedArray<IInGameCar>();

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
        var carsInRace = new ObservableUnlimitedArray<IInGameCar>();

        return new BackendGamemodeData
        {
            CurrentStage = backendStage,
            CarsInRace = carsInRace,
            raceState = RaceState.InProgress
        };
    }
}
