using NFMWorld.Reactor;
using NFMWorldLibrary.Backend.Gamemodes;
using static WorldXaml.UI.Yoga.Nodes;
using static NFMWorldLibrary.DriverInterface.UI.Elements.Nodes;

namespace NFMWorld.UI.Hud;

/// <summary>
/// Root HUD component. Receives state, provides it via context, and renders
/// all HUD sub-components. Instantiated by <see cref="DefaultHudManager"/>.
/// </summary>
public class HudHost(HudState state, params VNode[] children) : Component
{
    protected override VNode Render()
    {
        ProvideContext(HudState.Context, state);
        return View(children: children);
    }
}
