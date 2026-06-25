using NFMWorld.Reactor;
using WorldXaml.UI.Yoga;
using static WorldXaml.UI.Yoga.Nodes;

namespace NFMWorld.UI.Test;

public class HStack(VNode child, StackOrientation orientation = StackOrientation.Horizontal, float gapColumn = 0, float gapRow = 0) : Component
{
    protected override VNode Render()
    {
        return FlexPanel(
            flexDirection: orientation == StackOrientation.Horizontal
                ? YgFlexDirection.Row
                : YgFlexDirection.Column,
            alignItems: YgAlign.Center,
            gapColumn: gapColumn,
            gapRow: gapRow,
            children: child
        );
    }
}

public enum StackOrientation
{
    Horizontal,
    Vertical
}
