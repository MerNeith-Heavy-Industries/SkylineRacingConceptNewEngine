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
        var wrapped = children
            .Select(c => FlexPanel(position: YgPositionType.Absolute, children: c))
            .ToArray();
        return FlexPanel(flex: 1, position: YgPositionType.Relative, children: wrapped);
    }
}

