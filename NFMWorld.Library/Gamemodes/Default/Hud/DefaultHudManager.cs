using NFMWorld.Reactor;
using NFMWorld.UI.Hud;
using WorldXaml.UI.Yoga;
using static WorldXaml.UI.Yoga.Nodes;
using static NFMWorldLibrary.DriverInterface.UI.Elements.Nodes;

namespace NFMWorldLibrary.Backend.Gamemodes;

[ClientOnly]
public class DefaultHudManager : UIManager, IHud
{
    private readonly Reconciler _reconciler = new();
    private Visual? _root;

    public HudState State { get; private set; } = new();

    public DefaultHudManager()
    {
        RootPanel = new FlexPanel();
        UpdateHUD();
    }

    /// <summary>Push current state through the HUD component tree.</summary>
    public void UpdateHUD()
    {
        var host = HUDHost(state: State);
        _root = _reconciler.Reconcile(host, RootPanel, _root);
    }

    public void GameTick()
    {
        // Gamemodes update State externally, then call UpdateHUD()
    }

    void IHud.LayoutAndRender(Vector2 availableSize, Vector2? origin)
        => LayoutAndRender(availableSize, origin);
}