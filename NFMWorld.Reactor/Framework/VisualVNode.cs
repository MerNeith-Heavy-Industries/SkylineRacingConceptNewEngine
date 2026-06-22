namespace NFMWorld.Reactor;

/// <summary>
/// VNode base for native Yoga-backed nodes. Adds Name, Classes, and Children —
/// properties that map to a real <see cref="WorldXaml.UI.Yoga.Visual"/>.
/// Components use plain <see cref="VNode"/> instead.
/// </summary>
public abstract class VisualVNode(Type nodeType) : VNode(nodeType)
{
    public EquatableList<VNode>? Children { get; set; }
    public string? Classes { get; set; }
    public string? Name { get; set; }

    // ── Shared fluent builders ──────────────────────────────────────────

    public VisualVNode WithClasses(string? c) { Classes = c; return this; }
    public VisualVNode WithKey(object? k) { Key = k; return this; }
    public VisualVNode WithName(string? n) { Name = n; return this; }
}
