using NFMWorld.DriverInterface;
using NFMWorldLibrary.Gamemodes;
using NFMWorldLibrary.Util;
using WorldXaml.UI.Yoga.Events;

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

    /// <summary>
    /// Invoked when a key is pressed.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="keys">The state of all keys.</param>
    [ClientOnly]
    public void KeyPressed(Key key, in Keys keys);

    /// <summary>
    /// Invoked when a key is released.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="keys">The state of all keys.</param>
    [ClientOnly]
    public void KeyReleased(Key key, in Keys keys);

    /// <summary>
    /// Invoked when the mouse is moved.
    /// </summary>
    /// <param name="x">The X mouse position.</param>
    /// <param name="y">The Y mouse position.</param>
    /// <param name="buttons">The state of all buttons.</param>
    /// <param name="ctrlKey">Whether the Control key is being held.</param>
    /// <param name="shiftKey">Whether the Shift key is being held.</param>
    /// <param name="altKey">Whether the Alt key is being held.</param>
    [ClientOnly]
    public void MouseMoved(int x, int y, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey);

    /// <summary>
    /// Invoked when a mouse button is pressed.
    /// </summary>
    /// <param name="x">The X mouse position.</param>
    /// <param name="y">The Y mouse position.</param>
    /// <param name="button">The button that was pressed.</param>
    /// <param name="buttons">The state of all buttons.</param>
    /// <param name="ctrlKey">Whether the Control key is being held.</param>
    /// <param name="shiftKey">Whether the Shift key is being held.</param>
    /// <param name="altKey">Whether the Alt key is being held.</param>
    [ClientOnly]
    public void MousePressed(int x, int y, MouseButton button, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey);

    /// <summary>
    /// Invoked when a mouse button is released.
    /// </summary>
    /// <param name="x">The X mouse position.</param>
    /// <param name="y">The Y mouse position.</param>
    /// <param name="button">The button that was released.</param>
    /// <param name="buttons">The state of all buttons.</param>
    /// <param name="ctrlKey">Whether the Control key is being held.</param>
    /// <param name="shiftKey">Whether the Shift key is being held.</param>
    /// <param name="altKey">Whether the Alt key is being held.</param>
    [ClientOnly]
    public void MouseReleased(int x, int y, MouseButton button, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey);

    /// <summary>
    /// Invoked when a mouse button is released.
    /// </summary>
    /// <param name="x">The X mouse position.</param>
    /// <param name="y">The Y mouse position.</param>
    /// <param name="delta">The delta Y change.</param>
    /// <param name="buttons">The state of all buttons.</param>
    /// <param name="ctrlKey">Whether the Control key is being held.</param>
    /// <param name="shiftKey">Whether the Shift key is being held.</param>
    /// <param name="altKey">Whether the Alt key is being held.</param>
    [ClientOnly]
    public void MouseScrolled(int x, int y, int delta, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey);

    [ClientOnly]
    public void Render();

    #endregion
}