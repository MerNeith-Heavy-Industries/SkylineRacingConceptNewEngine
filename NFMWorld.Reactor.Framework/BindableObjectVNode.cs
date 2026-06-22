namespace NFMWorld.Reactor;

/// <summary>
/// VNode base for native Yoga-backed nodes. Adds Name, Classes, and Children —
/// properties that map to a real <see cref="WorldXaml.UI.Yoga.Visual"/>.
/// Components use plain <see cref="VNode"/> instead.
/// </summary>
public abstract class BindableObjectVNode : VNode
{
    public EquatableList<VNode>? Children { get; set; }
    public string? Classes { get; set; }
    public string? Name { get; set; }

    protected BindableObjectVNode(Type nodeType) : base(nodeType) { }

    // ── Shared fluent builders ──────────────────────────────────────────

    public BindableObjectVNode WithClasses(string? c) { Classes = c; return this; }
    public BindableObjectVNode WithKey(object? k) { Key = k; return this; }
    public BindableObjectVNode WithName(string? n) { Name = n; return this; }
}
