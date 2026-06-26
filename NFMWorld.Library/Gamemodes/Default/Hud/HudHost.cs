using NFMWorld.Reactor;
using NFMWorldLibrary.Backend.Gamemodes;
using WorldXaml.UI.Yoga;
using static WorldXaml.UI.Yoga.Nodes;
using static NFMWorld.Reactor.Nodes;
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
        return View(flex: 1, children: FlexPanel(flex: 1, position: Position.Relative, children: [..children.Select(child => FlexPanel(position: Position.Absolute, top: 0, right: 0, left: 0, bottom: 0, children: child))]));
    }
}
