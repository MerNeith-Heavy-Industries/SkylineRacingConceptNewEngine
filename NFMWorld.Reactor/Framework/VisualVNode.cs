using WorldXaml.UI;
using WorldXaml.UI.Yoga;

namespace NFMWorld.Reactor;

/// <summary>
/// VNode base for native Yoga-backed nodes. Adds Name, Classes, and Children —
/// properties that map to a real <see cref="Visual"/>.
/// Components use plain <see cref="VNode"/> instead.
/// </summary>
public abstract class VisualVNode : VNode
{
    public abstract Type NodeType { get; }

    public EquatableList<VNode>? Children { get; set; }
    public string? Classes { get; set; }
    public string? Name { get; set; }
    
    protected int TabOrder { get; set; }
    protected bool IsFocusable { get; set; }
    protected bool IsFocused { get; set; }

    // ── Shared fluent builders (Visual-level properties) ─────────────────

    public VisualVNode WithClasses(string? c) { Classes = c; return this; }
    public VisualVNode WithName(string? n) { Name = n; return this; }
    public new VisualVNode WithKey(object? value) { Key = value; return this; }
    public VisualVNode WithTabOrder(int value) { TabOrder = value; return this; }
    public VisualVNode WithIsFocusable(bool value) { IsFocusable = value; return this; }
    public VisualVNode WithIsFocused(bool value) { IsFocused = value; return this; }
    
    public abstract Visual CreateNative();
    public abstract void AssignProperties(Visual visual, ref BasePropertySnapshot? propertySnapshot);
}
