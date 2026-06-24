using WorldXaml.UI;

namespace NFMWorld.Reactor;

/// <summary>
/// Base virtual DOM node. Holds a native <see cref="System.Type"/> and optional key.
/// For Yoga-backed nodes, use <see cref="VisualVNode"/> which adds Name/Classes/Children.
/// Components extend this directly — they have no native backing, so Name/Classes don't apply.
/// </summary>
public abstract class VNode
{
    public abstract object? Key { get; set; }

    // ── Shared fluent builder ───────────────────────────────────────────

    public VNode WithKey(object? k) { Key = k; return this; }
}
