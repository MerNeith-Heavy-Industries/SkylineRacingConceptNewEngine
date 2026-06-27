using static NFMWorld.Reactor.Nodes;

namespace NFMWorld.Reactor;

/// <summary>
/// A component that renders children without participating in layout.
/// Equivalent to a pass-through container with <c>YGDisplayContents</c>.
/// Replaces the native <c>WorldXaml.UI.Yoga.ContentsPanel</c> (removed).
/// </summary>
public class ContentsPanel(params VNode[] children) : Component
{
    protected override VNode Render()
        => FlexPanel(display: Display.Contents, children: children);
}
