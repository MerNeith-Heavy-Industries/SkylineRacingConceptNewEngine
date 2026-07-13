using NFMWorld.DriverInterface;
using NFMWorldLibrary.Gamemodes;
using NFMWorldLibrary.Util;

namespace NFMWorldLibrary.Backend.Gamemodes;

public abstract class BaseGamemode(BaseGamemodeParameters gamemodeParameters, IGamemodeData gamemodeData) : IGamemode
{
    public IReadOnlyList<PlayerParameters> players => gamemodeParameters.Players;
    public UnlimitedArray<IInGameCar> carsInRace => gamemodeData.CarsInRace;
    public BackendStage currentStage => gamemodeData.CurrentStage;
    public int NumPlayers => players.Count;

    /// <summary>Per-frame HUD state pushed to the CEF overlay.</summary>
    public HudStateData HudState { get; protected set; } = new();

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
        // HUD rendering moved to CEF — no per-tick HUD updates needed.
    }

    public virtual void Reset()
    {
        
    }

    public virtual void KeyPressed(Key key, in Keys keys)
    {
        // Input routed to CEF via BasePhase — no HUD forwarding needed.
    }

    public virtual void KeyReleased(Key key, in Keys keys)
    {
    }

    public virtual void KeyTyped(char character)
    {
    }

    public virtual void MousePressed(int x, int y, MouseButton button, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
    }

    public virtual void MouseReleased(int x, int y, MouseButton button, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
    }

    public virtual void MouseScrolled(int x, int y, int delta, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
    }

    public virtual void MouseMoved(int x, int y, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
    }

    public virtual void Render()
    {
        // CEF handles UI rendering — no HUD LayoutAndRender needed.
    }
}