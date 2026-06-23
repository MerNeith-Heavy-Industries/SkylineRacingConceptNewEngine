using NFMWorld.Reactor;
using WorldXaml.UI.Yoga;
using static WorldXaml.UI.Yoga.Nodes;
using static NFMWorld.DriverInterface.UI.Nodes;

namespace NFMWorld.UI.Hud;

/// <summary>
/// Center-screen text overlay. Reads all state from <see cref="HUDContexts.Hud"/>.
/// </summary>
public class CentralTextView : Component
{
    protected override VNode Render()
    {
        var hud = UseContext(HUDContexts.Hud);
        return FlexPanel(
            position: YgPositionType.Absolute,
            top: 0f, left: 0f, right: 0f, bottom: 0f,
            alignItems: YgAlign.Center,
            flexDirection: YgFlexDirection.Column,
            children: [
                FlexPanel(alignItems: YgAlign.Center, flex: 1, children:
                    TextRun(
                        opacity: hud.CenterTextOpacity,
                        color: hud.CenterTextColor,
                        font: hud.CenterTextFont,
                        text: hud.CenterText,
                        strokeColor: hud.CenterTextStrokeColor,
                        display: YgDisplay.Flex
                    )
                ),
                FlexPanel(flex: 1) // spacer
            ]
        );
    }
}
