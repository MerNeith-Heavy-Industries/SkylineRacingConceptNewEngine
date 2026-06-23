using NFMWorld.Reactor;
using static WorldXaml.UI.Yoga.Nodes;
using static NFMWorldLibrary.DriverInterface.UI.Elements.Nodes;

namespace NFMWorld.UI.Hud;

/// <summary>
/// Root HUD component. Receives state, provides it via context, and renders
/// all HUD sub-components. Instantiated by <see cref="DefaultHudManager"/>.
/// </summary>
public class HUDHost : Component
{
    private readonly HudState _state;

    public HUDHost(HudState state)
    {
        _state = state;
    }

    protected override VNode Render()
    {
        ProvideContext(HUDContexts.Hud, _state);
        return View(children: [
            CentralTextView(),
            PowerDamageBars()
        ]);
    }
}
