namespace NFMWorld.Reactor;

/// <summary>
/// A VNode that hosts a user-defined <see cref="Component"/> subclass.
/// When reconciled, the component's <see cref="Component.Render"/> output
/// replaces this node in the native Yoga tree. The component itself has
/// no native representation — only its rendered output does.
/// </summary>
public abstract class ComponentNode : VNode
{
    /// <summary>The component <see cref="Type"/> to instantiate. Must extend <see cref="Component"/>.</summary>
    public abstract Type ComponentType { get; }

    /// <summary>
    /// The mounted component instance. Set by <see cref="Reconciler"/> on first reconciliation.
    /// </summary>
    public Component? Instance { get; internal set; }

    /// <summary>
    /// Creates a component VNode. The component is instantiated via
    /// <see cref="CreateComponent"/> when the reconciler first encounters this node.
    /// </summary>
    protected ComponentNode()
    {
    }

    /// <summary>
    /// Overridden by generated subclasses to create the component with typed constructor args.
    /// </summary>
    public abstract Component CreateComponent();
}

/// <summary>
/// Convenience factory for creating typed <see cref="ComponentNode"/> instances.
/// </summary>
public static class ComponentNodeFactory
{
    /// <summary>Create a component node for <typeparamref name="T"/>.</summary>
    public static ComponentNode Create<T>(params object?[]? args)
        where T : Component
    {
        if (args is null or { Length: 0 })
            return new UntypedComponentNode(typeof(T));
        return new UntypedComponentNode(typeof(T)) { Args = args };
    }
}

/// <summary>
/// Fallback <see cref="ComponentNode"/> for components without a generated wrapper.
/// Uses <see cref="Activator.CreateInstance(Type, object[])"/> with optional positional args.
/// </summary>
internal sealed class UntypedComponentNode(Type componentType) : ComponentNode
{
    public object?[]? Args { get; set; }

    public override Type ComponentType => componentType;

    public override Component CreateComponent()
        => (Component)Activator.CreateInstance(ComponentType, Args ?? [])!;
}
