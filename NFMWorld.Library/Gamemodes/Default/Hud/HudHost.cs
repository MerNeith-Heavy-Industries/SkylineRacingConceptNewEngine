using NFMWorld.Reactor;
using NFMWorldLibrary.Backend.Gamemodes;
using WorldXaml.UI.Yoga;
using static NFMWorld.Reactor.Nodes;
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
        // Each child is wrapped in an absolutely-positioned full-viewport
        // FlexPanel, placed as a direct child of the View. In Yoga,
        // absolutely positioned siblings are independent — they are removed
        // from normal flow and positioned relative to the parent's padding
        // box. Their order in the children array does not affect layout.
        return View(flex: 1, children: [
            ..children.Select(child =>
                FlexPanel(position: Position.Absolute, top: 0, right: 0, left: 0, bottom: 0, children: child))
        ]);
    }
}
