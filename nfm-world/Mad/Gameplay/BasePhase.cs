using NFMWorld.DriverInterface;
using NFMWorld.Reactor.Events;
using NFMWorld.Util;
using NFMWorldLibrary.Backend.Gamemodes;
using WorldXaml.UI.Yoga;
using WorldXaml.UI.Yoga.Events;

namespace NFMWorld.Gameplay;

public abstract class BasePhase
{
    protected List<UIManager> Uis = [];
    
    public FocusManager FocusManager { get; } = new();

    /// <summary>
    /// Whether the mouse was pressed this game tick. Reset at the end of a game tick.
    /// </summary>
    protected bool MouseDownThisFrame { get; private set; }

    /// <summary>
    /// Invoked at the beginning of a game tick.
    /// </summary>
    public virtual void BeginGameTick()
    {
    }

    /// <summary>
    /// Invoked at the middle of a game tick.
    /// </summary>
    public virtual void GameTick()
    {
    }

    /// <summary>
    /// Invoked at the end of a game tick.
    /// </summary>
    public virtual void EndGameTick()
    {
        MouseDownThisFrame = false;
    }

    /// <summary>
    /// Use <see cref="G"/> here to draw 2D overlays.
    /// Use <see cref="Scene"/> here to draw 3D content.
    /// </summary>
    public virtual void Render(float alpha)
    {
        foreach (var ui in Uis)
        {
            ui.LayoutAndRender(G.Viewport);
        }
    }

    /// <summary>
    /// Use ImGui methods in here.
    /// </summary>
    public virtual void RenderImgui()
    {
    }

    /// <summary>
    /// Renders after 2D overlays. Use to draw 3D content over 2D content.
    /// </summary>
    public virtual void Render3DOverlays()
    {
    }

    /// <summary>
    /// Invoked when <see cref="GameSparker.SetPhase"/> is called with the phase.
    /// </summary>
    public virtual void Enter()
    {
    }

    /// <summary>
    /// Invoked when <see cref="GameSparker.SetPhase"/> was called with the phase before, and is now being called with
    /// a new phase.
    /// </summary>
    public virtual void Exit()
    {
        FocusManager.ClearHover();
        FocusManager.ClearFocus();
    }

    /// <summary>
    /// Invoked when a key is pressed.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="imguiWantsKeyboard">If Imgui wants the keyboard.</param>
    /// <param name="keys">The state of all keys.</param>
    public virtual void KeyPressed(Key key, bool imguiWantsKeyboard, in Keys keys)
    {
        if (!imguiWantsKeyboard)
        {
            foreach (var ui in Uis)
            {
                ui.HandleKeyPressed(key, keys);
            }
        }
    }

    public virtual void KeyTyped(char character, bool imguiWantsKeyboard)
    {
        if (!imguiWantsKeyboard)
        {
            foreach (var ui in Uis)
            {
                ui.HandleKeyTyped(character);
            }
        }
    }

    /// <summary>
    /// Invoked when a key is released.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="imguiWantsKeyboard">If Imgui wants the keyboard.</param>
    /// <param name="keys">The state of all keys.</param>
    public virtual void KeyReleased(Key key, bool imguiWantsKeyboard, in Keys keys)
    {
        foreach (var ui in Uis)
        {
            ui.HandleKeyReleased(key, keys);
        }
    }

    /// <summary>
    /// Invoked when the mouse is moved.
    /// </summary>
    /// <param name="x">The X mouse position.</param>
    /// <param name="y">The Y mouse position.</param>
    /// <param name="imguiWantsMouse">If Imgui wants the mouse.</param>
    /// <param name="buttons">The state of all buttons.</param>
    /// <param name="ctrlKey">Whether the Control key is being held.</param>
    /// <param name="shiftKey">Whether the Shift key is being held.</param>
    /// <param name="altKey">Whether the Alt key is being held.</param>
    public virtual void MouseMoved(int x, int y, bool imguiWantsMouse, MouseButtons buttons, bool ctrlKey,
        bool shiftKey, bool altKey)
    {
        foreach (var ui in Uis)
        {
            ui.HandleMouseMoved(x, y, buttons, ctrlKey, shiftKey, altKey);
        }
    }

    /// <summary>
    /// Invoked when a mouse button is pressed.
    /// </summary>
    /// <param name="x">The X mouse position.</param>
    /// <param name="y">The Y mouse position.</param>
    /// <param name="imguiWantsMouse">If Imgui wants the mouse.</param>
    /// <param name="button">The button that was pressed.</param>
    /// <param name="buttons">The state of all buttons.</param>
    /// <param name="ctrlKey">Whether the Control key is being held.</param>
    /// <param name="shiftKey">Whether the Shift key is being held.</param>
    /// <param name="altKey">Whether the Alt key is being held.</param>
    public virtual void MousePressed(int x, int y, bool imguiWantsMouse, MouseButton button, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
        // Reset focus. Implementors can take over focus.
        FocusManager.FocusedElement = null;

        if (!imguiWantsMouse)
        {
            MouseDownThisFrame = true;
            foreach (var ui in Uis)
            {
                ui.HandleMousePressed(x, y, button, buttons, ctrlKey, shiftKey, altKey);
            }
        }
    }

    /// <summary>
    /// Invoked when a mouse button is released.
    /// </summary>
    /// <param name="x">The X mouse position.</param>
    /// <param name="y">The Y mouse position.</param>
    /// <param name="imguiWantsMouse">If Imgui wants the mouse.</param>
    /// <param name="button">The button that was released.</param>
    /// <param name="buttons">The state of all buttons.</param>
    /// <param name="ctrlKey">Whether the Control key is being held.</param>
    /// <param name="shiftKey">Whether the Shift key is being held.</param>
    /// <param name="altKey">Whether the Alt key is being held.</param>
    public virtual void MouseReleased(int x, int y, bool imguiWantsMouse, MouseButton button, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
        foreach (var ui in Uis)
        {
            ui.HandleMouseReleased(x, y, button, buttons, ctrlKey, shiftKey, altKey);
        }
    }

    /// <summary>
    /// Invoked when a mouse button is released.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="delta">The delta Y change.</param>
    /// <param name="imguiWantsMouse">If Imgui wants the mouse.</param>
    /// <param name="buttons">The state of all buttons.</param>
    /// <param name="ctrlKey">Whether the Control key is being held.</param>
    /// <param name="shiftKey">Whether the Shift key is being held.</param>
    /// <param name="altKey">Whether the Alt key is being held.</param>
    public virtual void MouseScrolled(int x, int y, int delta, bool imguiWantsMouse, MouseButtons buttons, bool ctrlKey,
        bool shiftKey, bool altKey)
    {
        if (!imguiWantsMouse)
        {
            foreach (var ui in Uis)
            {
                ui.HandleMouseScrolled(x, y, delta, buttons, ctrlKey, shiftKey, altKey);
            }
        }
    }

    public virtual void WindowSizeChanged(int width, int height)
    {
    }
}