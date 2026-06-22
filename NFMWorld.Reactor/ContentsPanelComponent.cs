using static WorldXaml.UI.Yoga.Nodes;

namespace NFMWorld.Reactor;

/// <summary>
/// A component that renders children without participating in layout.
/// Equivalent to a pass-through container with <c>YGDisplayContents</c>.
/// Replaces the native <c>WorldXaml.UI.Yoga.ContentsPanel</c> (removed).
/// </summary>
public class ContentsPanelComponent(params VNode[] children) : Component
{
    protected override VNode Render()
        => FlexPanel(display: YgDisplay.Contents, children: children);
}
