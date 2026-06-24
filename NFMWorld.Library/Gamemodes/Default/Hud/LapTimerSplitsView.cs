using NFMWorld.DriverInterface;
using NFMWorld.Reactor;
using WorldXaml.UI.Yoga;
using static NFMWorld.DriverInterface.FontFamily;
using static NFMWorld.DriverInterface.FontStyle;
using static WorldXaml.UI.Yoga.Nodes;
using static NFMWorld.DriverInterface.UI.Nodes;

namespace NFMWorld.UI.Hud;

/// <summary>
/// Displays current lap / total laps text. Reads state from <see cref="HudState.Context"/>.
/// </summary>
public class LapTimerSplitsView : Component
{
    protected override VNode Render()
    {
        var hud = UseContext(HudState.Context);
        return FlexPanel(
            flexDirection: YgFlexDirection.Column,
            alignItems: YgAlign.FlexStart,
            justifyContent: YgJustify.Center,
            gap: 10f,
            padding: 10f,
            children: [
                FlexPanel(flexDirection: YgFlexDirection.Row, children:
                    TextRun(flex: 1, fontFamily: Adventure, fontStyle: Bold, fontSize: 24, foreground: Color.White, stroke: Color.Black,
                        text: $"Lap: {hud.CurrentLap}/{hud.TotalLaps}")
                ),
                FlexPanel(flexDirection: YgFlexDirection.Row, children: [
                    TextRun(flex: 1, fontStyle: Bold, fontSize: 24, foreground: Color.White, stroke: Color.Black, elements: [
                        Run(fontFamily: Adventure, text: "Time: "),
                        Run(fontFamily: DroidSans, text: hud.TimeText)
                    ])
                ]),
                FlexPanel(display: hud.LapTimeText != "" ? YgDisplay.Flex : YgDisplay.None, children: [
                    TextRun(flex: 1, fontStyle: Bold, fontSize: 24, foreground: Color.White, stroke: Color.Black, elements: [
                        Run(fontFamily: Adventure, text: "Lap Time: "),
                        Run(fontFamily: DroidSans, text: hud.LapTimeText)
                    ])
                ]),
                FlexPanel(display: hud.CheckpointSplitsText != "" ? YgDisplay.Flex : YgDisplay.None, children: [
                    TextRun(flex: 1, fontStyle: Bold, fontSize: 24, foreground: hud.CheckpointSplitsColor ?? Color.White, stroke: Color.Black, elements: [
                        Run(fontFamily: Adventure, text: "CHK Diff: "),
                        Run(fontFamily: DroidSans, text: hud.CheckpointSplitsText)
                    ])
                ]),
                FlexPanel(display: hud.LapSplitsText != "" ? YgDisplay.Flex : YgDisplay.None, children: [
                    TextRun(flex: 1, fontStyle: Bold, fontSize: 24, foreground: hud.CheckpointSplitsColor ?? Color.White, stroke: Color.Black, elements: [
                        Run(fontFamily: Adventure, text: "Lap Diff: "),
                        Run(fontFamily: DroidSans, text: hud.LapSplitsText)
                    ])
                ])
            ]
        );
    }
}
