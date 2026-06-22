using WorldXaml.UI;

namespace NFMWorld.Reactor;

/// <summary>
/// Base class for virtual DOM nodes. Subclasses are generated per Yoga node type
/// with strongly-typed With* methods for each [Property].
/// </summary>
public class VNode
{
    public Type NodeType { get; protected init; }
    public Dictionary<int, object?>? Properties { get; set; }
    public EquatableList<VNode>? Children { get; set; }
    public string? Classes { get; set; }
    public object? Key { get; set; }
    public string? Name { get; set; }

    protected VNode(Type nodeType) { NodeType = nodeType; }

    // ── Internal helpers used by generated subclasses ───────────────────

    protected TNode SetProp<TNode, TProp>(Property property, TProp value)
        where TNode : VNode where TProp : class
    {
        Properties ??= [];
        Properties[property.Id] = value;
        return (TNode)this;
    }

    protected TNode SetPropVal<TNode, TProp>(Property property, TProp value)
        where TNode : VNode where TProp : struct
    {
        Properties ??= [];
        Properties[property.Id] = value;
        return (TNode)this;
    }

    protected TNode SetPropValNullable<TNode, TProp>(Property property, TProp? value)
        where TNode : VNode where TProp : struct
    {
        Properties ??= [];
        Properties[property.Id] = value;
        return (TNode)this;
    }

    // ── Shared fluent builders ──────────────────────────────────────────

    public VNode WithClasses(string? c) { Classes = c; return this; }
    public VNode WithKey(object? k) { Key = k; return this; }
}
