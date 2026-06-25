using NFMWorld.Reactor;
using WorldXaml.UI.Yoga;
using static WorldXaml.UI.Yoga.Nodes;

namespace NFMWorld.UI.Menu;

public class GarageUiView : Component
{
    private readonly float[] _barValues = new float[9];

    public GarageUiView()
    {
        DisableMemo();
    }

    public void SetBarValue(int index, float value)
    {
        if (index >= 0 && index < _barValues.Length)
            _barValues[index] = value;
    }

    protected override VNode Render()
    {
        return View(
            name: "LapTimerSplits",
            flexDirection: YgFlexDirection.Column,
            alignItems: YgAlign.FlexStart,
            justifyContent: YgJustify.FlexStart,
            gap: 10f,
            padding: 10f,
            children: [
                MakeBarRow(0, "Top Speed", 1, "Acceleration", 2, "Handling"),
                MakeBarRow(3, "Power Save", 4, "Strength", 5, "Max Health"),
                MakeBarRow(6, "Stunting", 7, "Hypergliding", 8, "AB'ing")
            ]
        );
    }

    private VNode MakeBarRow(int i0, string n0, int i1, string n1, int i2, string n2)
    {
        return FlexPanel(flexDirection: YgFlexDirection.Row, gap: 25f, marginBottom: 25f, children: [
            Bar(i0, n0), Bar(i1, n1), Bar(i2, n2)
        ]);
    }

    private VNode Bar(int index, string name)
    {
        var pct = (int)(Math.Clamp(_barValues[index], 0f, 1f) * 100);
        return FlexPanel(name: $"{name}: {pct}%", width: 120f, height: 40f,
            flexDirection: YgFlexDirection.Column, alignItems: YgAlign.Center);
    }
}