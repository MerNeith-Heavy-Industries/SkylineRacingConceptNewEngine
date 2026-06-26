using NFMWorld.DriverInterface;
using NFMWorld.Reactor;
using NFMWorldLibrary.Backend.Gamemodes;
using WorldXaml.UI.Yoga;
using static WorldXaml.UI.Yoga.Nodes;
using static NFMWorld.DriverInterface.UI.Nodes;

namespace NFMWorld.UI.Hud;

/// <summary>
/// Power and damage meter bars. Reads state from <see cref="HudState.Context"/>.
/// </summary>
public class PowerDamageBars : Component
{
    [ClientOnly]
    protected override VNode Render()
    {
        var hud = UseContext(HudState.Context);
        return FlexPanel(
            position: Position.Absolute,
            top: 0f, right: 0f,
            padding: 10f,
            flexDirection: FlexDirection.Column,
            gap: 10f,
            alignItems: Align.FlexEnd,
            children: [
                MeasureBar(fillAmount: hud.DamageFillAmount, color: hud.DamageColor, scale: 1.2f, imageData: G.LoadImage("data/images/damage.gif")),
                MeasureBar(fillAmount: hud.PowerFillAmount, color: hud.PowerColor, scale: 1.2f, imageData: G.LoadImage("data/images/power.gif"))
            ]
        );
    }
}