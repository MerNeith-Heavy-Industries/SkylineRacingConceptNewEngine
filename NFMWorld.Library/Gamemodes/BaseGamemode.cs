using NFMWorld.DriverInterface;
using NFMWorld.Reactor.Events;
using NFMWorldLibrary.Gamemodes;
using NFMWorldLibrary.Util;
using WorldXaml.UI.Yoga.Events;

namespace NFMWorldLibrary.Backend.Gamemodes;

public abstract class BaseGamemode(BaseGamemodeParameters gamemodeParameters, IGamemodeData gamemodeData) : IGamemode
{
    public IReadOnlyList<PlayerParameters> players => gamemodeParameters.Players;
    public UnlimitedArray<IInGameCar> carsInRace => gamemodeData.CarsInRace;
    public BackendStage currentStage => gamemodeData.CurrentStage;
    public int NumPlayers => players.Count;

    [ClientOnly]
    protected DefaultHudManager Hud = new()
    {
        FocusManager = gamemodeData.FocusManager
    };

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
        ClientServer.RunIfOnClient(Hud.GameTick);
    }

    public virtual void Reset()
    {
        
    }

    public virtual void KeyPressed(Key key, in Keys keys)
    {
        Hud.HandleKeyPressed(key, keys);
    }

    public virtual void KeyReleased(Key key, in Keys keys)
    {
        Hud.HandleKeyReleased(key, keys);
    }

    public virtual void KeyTyped(char character)
    {
        Hud.HandleKeyTyped(character);
    }

    public virtual void MousePressed(int x, int y, MouseButton button, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
        Hud.HandleMousePressed(x, y, button, buttons, ctrlKey, shiftKey, altKey);
    }

    public virtual void MouseReleased(int x, int y, MouseButton button, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
        Hud.HandleMouseReleased(x, y, button, buttons, ctrlKey, shiftKey, altKey);
    }

    public virtual void MouseScrolled(int x, int y, int delta, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
        Hud.HandleMouseScrolled(x, y, delta, buttons, ctrlKey, shiftKey, altKey);
    }

    public virtual void MouseMoved(int x, int y, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
        Hud.HandleMouseMoved(x, y, buttons, ctrlKey, shiftKey, altKey);
    }

    public virtual void Render()
    {
        Hud.LayoutAndRender(G.Viewport);
    }
}