using NFMWorld.Reactor;
using WorldXaml.UI.Yoga;
using static NFMWorld.Reactor.Nodes;
using static NFMWorld.DriverInterface.UI.Nodes;

namespace NFMWorld.UI.Hud;

/// <summary>
/// Center-screen text overlay. Reads all state from <see cref="HudState.Context"/>.
/// </summary>
public class CentralTextView : Component
{
    protected override VNode Render()
    {
        var hud = UseContext(HudState.Context);
        return FlexPanel(
            position: Position.Absolute,
            top: 0f, left: 0f, right: 0f, bottom: 0f,
            alignItems: Align.Center,
            flexDirection: FlexDirection.Column,
            children: [
                FlexPanel(alignItems: Align.Center, flex: 1, children:
                    TextRun(
                        opacity: hud.CenterTextOpacity,
                        foreground: hud.CenterTextColor,
                        fontFamily: hud.CenterTextFontFamily,
                        fontStyle: hud.CenterTextFontStyle,
                        fontSize: hud.CenterTextFontSize,
                        text: hud.CenterText,
                        stroke: hud.CenterTextStrokeColor,
                        display: Display.Flex
                    )
                ),
                FlexPanel(flex: 1) // spacer
            ]
        );
    }
}
