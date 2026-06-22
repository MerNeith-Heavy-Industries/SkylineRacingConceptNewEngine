using NFMWorld.Reactor;
using static WorldXaml.UI.Yoga.Nodes;
using Yoga;

namespace NFMWorld.Reactor;

/// <summary>
/// A component that renders children without participating in layout.
/// Equivalent to a pass-through container with <c>YGDisplayContents</c>.
/// Replaces the native <c>WorldXaml.UI.Yoga.ContentsPanel</c> (removed).
/// </summary>
public class ContentsPanelComponent : Component
{
    private readonly VNode[] _children;

    public ContentsPanelComponent(params VNode[] children)
    {
        _children = children;
    }

    protected override VNode Render()
        => FlexPanel(display: YgDisplay.Contents, children: _children);
}
