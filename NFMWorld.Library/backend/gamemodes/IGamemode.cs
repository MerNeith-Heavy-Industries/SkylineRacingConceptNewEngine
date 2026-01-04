using NFMWorld.Library.backend;
using NFMWorld.Mad;
using NFMWorld.Mad.gamemodes;
using NFMWorld.Util;

public interface IGamemode
{
    public int playerCarIndex { get; }
    public IReadOnlyList<PlayerParameters> players { get; }
    public PlayerParameters player { get; }
    public UnlimitedArray<IInGameCar> carsInRace { get; }
    public BackendStage currentStage { get; }
    public int NumPlayers { get; }
    public event EventHandler<byte[]>? RaceFinished;
    public void Enter();
    public void Exit();
    public void GameTick();
    public void Reset();
}