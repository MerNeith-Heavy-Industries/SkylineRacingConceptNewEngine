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

    public sealed override object? Key
    {
        get => _key;
        set => _key = value;
    }

    public string? Name
    {
        get => _name;
        set => _name = value;
    }

    // Do not rename (source generator uses it)
    // ReSharper disable InconsistentNaming
    protected Action<VisualTreeAttachmentEventArgs>? _attachedToVisualTree;
    protected Action<VisualTreeAttachmentEventArgs>? _detachedFromVisualTree;
    protected string? _classes;
    protected object? _key;
    protected string? _name;
    protected int? _tabOrder;
    protected bool? _isFocusable;
    protected bool? _isFocused;
    // ReSharper restore InconsistentNaming

    // ── Shared fluent builders (Visual-level properties) ─────────────────

    public VisualVNode WithClasses(string? c) { _classes = c; return this; }
    public VisualVNode WithName(string? n) { _name = n; return this; }
    public new VisualVNode WithKey(object? value) { Key = value; return this; }
    public VisualVNode WithTabOrder(int value) { _tabOrder = value; return this; }
    public VisualVNode WithIsFocusable(bool value) { _isFocusable = value; return this; }
    public VisualVNode WithIsFocused(bool value) { _isFocused = value; return this; }
    
    /// <summary>
    /// Creates the native <see cref="Visual"/> type of this <see cref="VNode"/>.
    /// </summary>
    /// <returns>The created node.</returns>
    public abstract Visual CreateNative();
    
    /// <summary>
    /// Assigns the properties of this <see cref="VNode"/> to the <see cref="Visual"/>.
    /// </summary>
    /// <param name="visual">The visual node</param>
    /// <param name="propertySnapshot">
    /// A reference to a property snapshot which will receive the previous values of changed properties. If <c>null</c>,
    /// a new instance is created. It must have been created with this VNode type.
    /// </param>
    public abstract void AssignProperties(Visual visual, ref BasePropertySnapshot? propertySnapshot);
}
