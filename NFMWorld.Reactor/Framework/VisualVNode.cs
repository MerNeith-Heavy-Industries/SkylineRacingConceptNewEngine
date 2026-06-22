namespace NFMWorld.Reactor;

/// <summary>
/// VNode base for native Yoga-backed nodes. Adds Name, Classes, and Children —
/// properties that map to a real <see cref="WorldXaml.UI.Yoga.Visual"/>.
/// Components use plain <see cref="VNode"/> instead.
/// </summary>
public abstract class VisualVNode : VNode
{
    public abstract Type NodeType { get; }

    public EquatableList<VNode>? Children { get; set; }
    public string? Classes { get; set; }
    public string? Name { get; set; }

    public Dictionary<int, object?>? Properties { get; set; }

    // ── Internal helpers used by generated subclasses ───────────────────

    protected TNode SetProp<TNode, TProp>(Property property, TProp value)
        where TNode : VisualVNode where TProp : class
    {
        Properties ??= [];
        Properties[property.Id] = value;
        return (TNode)this;
    }

    protected TNode SetPropVal<TNode, TProp>(Property property, TProp value)
        where TNode : VisualVNode where TProp : struct
    {
        Properties ??= [];
        Properties[property.Id] = value;
        return (TNode)this;
    }

    protected TNode SetPropValNullable<TNode, TProp>(Property property, TProp? value)
        where TNode : VisualVNode where TProp : struct
    {
        Properties ??= [];
        Properties[property.Id] = value;
        return (TNode)this;
    }
    
    // ── Shared fluent builders ──────────────────────────────────────────

    public VisualVNode WithClasses(string? c) { Classes = c; return this; }
    public VisualVNode WithName(string? n) { Name = n; return this; }
    
    public abstract Visual CreateNative();
}
