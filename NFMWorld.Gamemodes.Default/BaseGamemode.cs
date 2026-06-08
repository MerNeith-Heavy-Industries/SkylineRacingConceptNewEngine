using NFMWorldLibrary.Util;

namespace NFMWorldLibrary.Backend.Gamemodes;

public abstract class BaseGamemode(BaseGamemodeParameters gamemodeParameters, IGamemodeData gamemodeData) : IGamemode
{
    public IReadOnlyList<PlayerParameters> players => gamemodeParameters.Players;
    public UnlimitedArray<IInGameCar> carsInRace => gamemodeData.CarsInRace;
    public BackendStage currentStage => gamemodeData.CurrentStage;
    public int NumPlayers => players.Count;

    /// <summary>
    /// Arguments: byte[] player standings indexed by player index
    /// </summary>
    public abstract event EventHandler<byte[]>? RaceFinished;

    public virtual void Enter()
    {
        
    }

    public virtual void Exit()
    {
        
    }

    public virtual void GameTick()
    {

    }

    public virtual void Reset()
    {
        
    }
}