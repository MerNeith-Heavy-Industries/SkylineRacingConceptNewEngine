using NFMWorld.DriverInterface;
using NFMWorldLibrary.Util;

namespace NFMWorldLibrary.Backend.Gamemodes;

public interface IGamemode
{
    public IReadOnlyList<PlayerParameters> players { get; }
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

    #region Client

    [ClientOnly]
    public void KeyPressed(Keys key)
    {
        
    }

    [ClientOnly]
    public void KeyReleased(Keys key)
    {
        
    }

    [ClientOnly]
    public void Render()
    {
        
    }

    #endregion
}