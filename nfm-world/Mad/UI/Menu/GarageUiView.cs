using System.Collections.Immutable;
using NFMWorld.DriverInterface.UI;
using NFMWorld.Reactor;
using WorldXaml.UI.Yoga;
using static NFMWorld.Reactor.Nodes;
using static NFMWorld.DriverInterface.UI.Nodes;

namespace NFMWorld.UI.Menu;

public class GarageUiView(float topSpeed, float acceleration, float handling, float powerSave, float strength, float maxHelath, float stunting, float hypergliding, float abing) : Component
{
    protected override VNode Render()
    {
        return View(
            name: "GarageUiView",
            flexDirection: FlexDirection.Column,
            alignItems: Align.FlexStart,
            justifyContent: Justify.FlexStart,
            gap: 10f,
            padding: 100f,
            children: [
                FlexPanel(flexDirection: FlexDirection.Row, gap: 25f, marginBottom: 25f, children: [
                    GarageDynamicStatBar(statName: "Top Speed", targetValue: topSpeed),
                    GarageDynamicStatBar(statName: "Acceleration", targetValue: acceleration),
                    GarageDynamicStatBar(statName: "Handling", targetValue: handling)
                ]),
                FlexPanel(flexDirection: FlexDirection.Row, gap: 25f, marginBottom: 25f, children: [
                    GarageDynamicStatBar(statName: "Power Save", targetValue: powerSave),
                    GarageDynamicStatBar(statName: "Strength", targetValue: strength),
                    GarageDynamicStatBar(statName: "Max Health", targetValue: maxHelath)
                ]),
                FlexPanel(flexDirection: FlexDirection.Row, gap: 25f, marginBottom: 25f, children: [
                    GarageDynamicStatBar(statName: "Stunting", targetValue: stunting),
                    GarageDynamicStatBar(statName: "Hypergliding", targetValue: hypergliding),
                    GarageDynamicStatBar(statName: "AB'ing", targetValue: abing)
                ])
            ]
        );
    }
}