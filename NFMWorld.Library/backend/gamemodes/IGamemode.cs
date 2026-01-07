using nfm_world_library.mad;
using nfm_world_library.util;

namespace nfm_world_library.backend.gamemodes;

public interface IGamemode
{
    public int playerCarIndex { get; }
    public IReadOnlyList<PlayerParameters> players { get; }
    public PlayerParameters player { get; }
    public UnlimitedArray<IInGameCar> carsInRace { get; }
    public BackendStage currentStage { get; }
    public int NumPlayers { get; }
    
    /// <summary>
    /// Arguments: byte[] player standings indexed by player index
    /// </summary>
    public event EventHandler<byte[]>? RaceFinished;

    public void Enter();
    public void Exit();
    public void GameTick();
    public void Reset();
}