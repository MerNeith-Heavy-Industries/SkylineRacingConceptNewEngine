using NFMWorld.DriverInterface;
using NFMWorld.Reactor;
using WorldXaml.UI.Yoga;
using static WorldXaml.UI.Yoga.Nodes;

namespace NFMWorld.UI.Hud;

/// <summary>
/// Displays current lap / total laps text. Reads state from <see cref="HUDContexts.Hud"/>.
/// </summary>
public class LapTimerSplitsView : Component
{
    protected override VNode Render()
    {
        var hud = UseContext(HUDContexts.Hud);
        return FlexPanel(
            flexDirection: YgFlexDirection.Row,
            children:
                TextRun(
                    font: Font.Parse("bold 24px Adventure"),
                    color: Color.White,
                    strokeColor: Color.Black,
                    flex: 1,
                    text: $"Lap: {hud.CurrentLap}/{hud.TotalLaps}"
                )
        );
    }
}
