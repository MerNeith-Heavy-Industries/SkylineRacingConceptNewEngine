using NFMWorld.Reactor;
using WorldXaml.UI.Yoga;
using static NFMWorld.Reactor.Nodes;

namespace NFMWorld.UI.Test;

public class HStack(VNode child, StackOrientation orientation = StackOrientation.Horizontal, float gapColumn = 0, float gapRow = 0) : Component
{
    protected override VNode Render()
    {
        return FlexPanel(
            flexDirection: orientation == StackOrientation.Horizontal
                ? FlexDirection.Row
                : FlexDirection.Column,
            alignItems: Align.Center,
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
