using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace NFMWorld.Reactor;

/// <summary>
/// Represents a container node that can hold multiple child nodes and arrange them according to the Flexbox layout
/// algorithm.
/// </summary>
[DebuggerDisplay("{DebugToString()}")]
public class FlexPanel : Node
{
    [Content]
    public NodeChildCollection Children { get; }

    public override IReadOnlyList<Visual> VisualChildren => Children;

    // ── Visual children API ────────────────────────────────────────────
    public override bool CanHaveChildren => true;
    public override void AddChild(Visual child) => Children.Add((Node)child);
    public override void InsertAt(int index, Visual child) => Children.Insert(index, (Node)child);
    public override void RemoveAt(int index) => Children.RemoveAt(index);

    public FlexPanel()
    {
        Children = new NodeChildCollection(this);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override string DebugToString()
    {
        var sb = new StringBuilder();
        sb.Append($"FlexPanel(Name={Name}, LayoutX={LayoutX}, LayoutY={LayoutY}, LayoutWidth={LayoutWidth}, LayoutHeight={LayoutHeight})");
        foreach (var child in Children)
        {
            sb.AppendLine();
            sb.Append('{');
            sb.Append((child is Node node ? node.DebugToString() : child.ToString() ?? "").Replace("\n", "\n  "));
            sb.Append('}');
        }
        return sb.ToString();
    }
}