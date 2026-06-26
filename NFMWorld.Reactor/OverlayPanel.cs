using System.Linq;
using static WorldXaml.UI.Yoga.Nodes;
using Yoga;

namespace NFMWorld.Reactor;

/// <summary>
/// A component that renders its children with absolute positioning overlaid
/// on a relative-positioned container. Equivalent to a CSS overlay panel.
/// </summary>
public class OverlayPanel(params VNode[] children) : Component
{
    protected override VNode Render()
    {
        foreach (var child in children)
        {
            if (child is FlexPanelNode flexPanel)
            {
                flexPanel.WithPosition(Position.Absolute);
            }            
        }

        return FlexPanel(flex: 1, position: Position.Relative, children: children);
    }
}
