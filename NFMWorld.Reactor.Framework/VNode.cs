using WorldXaml.UI;

namespace NFMWorld.Reactor;

/// <summary>
/// Base virtual DOM node. Holds a native <see cref="System.Type"/> and optional key.
/// For Yoga-backed nodes, use <see cref="BindableObjectVNode"/> which adds Name/Classes/Children.
/// Components extend this directly — they have no native backing, so Name/Classes don't apply.
/// </summary>
public class VNode
{
    public Type NodeType { get; protected init; }
    public Dictionary<int, object?>? Properties { get; set; }
    public object? Key { get; set; }

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

    // ── Shared fluent builder ───────────────────────────────────────────

    public VNode WithKey(object? k) { Key = k; return this; }
}
