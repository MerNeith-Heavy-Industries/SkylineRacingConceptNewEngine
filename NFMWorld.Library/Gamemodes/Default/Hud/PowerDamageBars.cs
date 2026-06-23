using NFMWorld.Reactor;
using WorldXaml.UI.Yoga;
using static WorldXaml.UI.Yoga.Nodes;
using static NFMWorld.DriverInterface.UI.Nodes;

namespace NFMWorld.UI.Hud;

/// <summary>
/// Power and damage meter bars. Reads state from <see cref="HUDContexts.Hud"/>.
/// </summary>
public class PowerDamageBars : Component
{
    protected override VNode Render()
    {
        var hud = UseContext(HUDContexts.Hud);
        return FlexPanel(
            position: YgPositionType.Absolute,
            top: 0f, right: 0f,
            padding: 10f,
            flexDirection: YgFlexDirection.Column,
            gap: 10f,
            alignItems: YgAlign.FlexEnd,
            children: [
                FlexPanel(children: MeasureBar(fillAmount: hud.DamageFillAmount, color: hud.DamageColor, scale: 1.2f)),
                FlexPanel(children: MeasureBar(fillAmount: hud.PowerFillAmount, color: hud.PowerColor, scale: 1.2f))
            ]
        );
    }
}