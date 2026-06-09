using NFMWorld.DriverInterface;
using NFMWorld.UI.Hud;
using WorldXaml.UI.Yoga;
using WorldXaml.UI.Yoga.Events;

namespace NFMWorldLibrary.Backend.Gamemodes;

[ClientOnly]
public class DefaultHudManager : IHud
{
    private readonly FlexPanel _rootPanel = new();
    private readonly OverlayPanel _overlay = new();
    public HudViewModel DataContext { get; set; } = new();
    public required FocusManager FocusManager { get; set; }

    public DefaultHudManager()
    {
        _rootPanel.Children.Add(_overlay);
        
        _overlay.ContentChildren.Add(new PowerDamageBars());
    }
    
    public void LayoutAndRender(System.Numerics.Vector2 availableSize, System.Numerics.Vector2? origin = null)
    {
        _rootPanel.LayoutAndRender(availableSize, origin);
    }

    public void HandleKeyPressed(Key key, in Keys keys)
    {
        _rootPanel.KeyPressed(FocusManager, new KeyboardEvent(key, IBackend.Backend.GetKeyFromScancode(key), keys));
    }

    public void HandleKeyReleased(Key key, in Keys keys)
    {
        _rootPanel.KeyReleased(FocusManager, new KeyboardEvent(key, IBackend.Backend.GetKeyFromScancode(key), keys));
    }

    public void HandleMouseMoved(int x, int y, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
        _rootPanel.MouseMoved(FocusManager, new BaseMouseMoveEvent(new System.Numerics.Vector2(x, y), buttons, ctrlKey, altKey, shiftKey));
    }

    public void HandleMousePressed(int x, int y, MouseButton button, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
        if (FocusManager.HitTest(_rootPanel, new System.Numerics.Vector2(x, y)) is { } visual)
        {
            FocusManager.FocusedElement = visual;
        }

        _rootPanel.MousePressed(FocusManager, new BaseMouseEvent(new System.Numerics.Vector2(x, y), button, buttons, ctrlKey, altKey, shiftKey));
    }

    public void HandleMouseReleased(int x, int y, MouseButton button, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
        _rootPanel.MouseReleased(FocusManager, new BaseMouseEvent(new System.Numerics.Vector2(x, y), button, buttons, ctrlKey, altKey, shiftKey));
    }

    public void HandleMouseScrolled(int x, int y, int delta, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
        _rootPanel.MouseScrolled(FocusManager, new BaseMouseWheelEvent(new System.Numerics.Vector3(0, delta, 0), new System.Numerics.Vector2(x, y), buttons, ctrlKey, altKey, shiftKey));
    }
}