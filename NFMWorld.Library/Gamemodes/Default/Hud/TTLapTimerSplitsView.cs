using NFMWorld.Reactor;
using static WorldXaml.UI.Yoga.Nodes;

namespace NFMWorld.UI.Hud;

/// <summary>
/// Time Trial lap timer splits view. Reads state from <see cref="HUDContexts.Hud"/>.
/// </summary>
public class TTLapTimerSplitsView : Component
{
    protected override VNode Render()
    {
        var hud = UseContext(HUDContexts.Hud);
        return FlexPanel(
            flexDirection: YgFlexDirection.Column,
            alignItems: YgAlign.FlexStart,
            justifyContent: YgJustify.Center,
            gap: 10f,
            padding: 10f,
            children: [
                FlexPanel(flexDirection: YgFlexDirection.Row, children:
                    TextRun(font: Font.Parse("bold 24px Adventure"), color: Color.White, strokeColor: Color.Black, flex: 1,
                        text: $"Lap: {hud.CurrentLap}/{hud.TotalLaps}")
                ),
                FlexPanel(flexDirection: YgFlexDirection.Row, children: [
                    TextRun(font: Font.Parse("bold 24px Adventure"), color: Color.White, strokeColor: Color.Black, flex: 1, text: "Time:"),
                    TextRun(font: Font.Parse("bold 24px DroidSans"), color: Color.White, strokeColor: Color.Black, flex: 1, text: hud.TimeText)
                ]),
                FlexPanel(children:
                    TextRun(font: Font.Parse("bold 24px DroidSans"), color: Color.White, strokeColor: Color.Black, flex: 1, text: hud.LapTimeText)
                ),
                FlexPanel(children:
                    TextRun(font: Font.Parse("bold 24px DroidSans"), color: Color.White, strokeColor: Color.Black, flex: 1, text: hud.CheckpointSplitsText)
                )
            ]
        );
    }
}
