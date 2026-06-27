using NFMWorld.DriverInterface;
using NFMWorld.Reactor;
using NFMWorld.Reactor.Events;
using WorldXaml.UI.Yoga;
using WorldXaml.UI.Yoga.Events;

namespace NFMWorldLibrary.Backend.Gamemodes;

[ClientOnly]
public class UIManager
{
    public FlexPanel RootPanel { get; init; } = new();
    public required FocusManager FocusManager { get; set; }

    public void LayoutAndRender(Vector2 availableSize, Vector2? origin = null)
    {
        RootPanel.LayoutAndRender(availableSize, origin);
    }

    public void HandleKeyPressed(Key key, in Keys keys)
    {
        RootPanel.DispatchKeyPressed(FocusManager, new KeyboardEvent(key, IBackend.Backend.GetKeyFromScancode(key), keys));
    }

    public void HandleKeyReleased(Key key, in Keys keys)
    {
        RootPanel.DispatchKeyReleased(FocusManager, new KeyboardEvent(key, IBackend.Backend.GetKeyFromScancode(key), keys));
    }

    public void HandleKeyTyped(char character)
    {
        RootPanel.DispatchKeyTyped(FocusManager, new KeyboardTypingEvent(character));
    }

    public void HandleMouseMoved(int x, int y, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
        FocusManager.DispatchMouseMove(RootPanel,
            new BaseMouseMoveEvent(new Vector2(x, y), buttons, ctrlKey, altKey, shiftKey));
    }

    public void HandleMousePressed(int x, int y, MouseButton button, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
        if (FocusManager.HitTest(RootPanel, new Vector2(x, y)) is { } visual)
        {
            FocusManager.FocusedElement = visual;
        }

        RootPanel.DispatchMousePressed(FocusManager, new BaseMouseEvent(new Vector2(x, y), button, buttons, ctrlKey, altKey, shiftKey));
    }

    public void HandleMouseReleased(int x, int y, MouseButton button, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
        RootPanel.DispatchMouseReleased(FocusManager, new BaseMouseEvent(new Vector2(x, y), button, buttons, ctrlKey, altKey, shiftKey));
    }

    public void HandleMouseScrolled(int x, int y, int delta, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
        RootPanel.DispatchMouseScrolled(FocusManager, new BaseMouseWheelEvent(new System.Numerics.Vector3(0, delta, 0), new Vector2(x, y), buttons, ctrlKey, altKey, shiftKey));
    }
}