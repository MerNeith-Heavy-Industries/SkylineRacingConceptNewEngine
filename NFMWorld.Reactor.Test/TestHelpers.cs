namespace NFMWorld.Reactor.Test;

/// <summary>
/// No-op synchronization context for backward compatibility with tests
/// that were written to call <see cref="Drain"/>. Since the reconciler
/// is now synchronous, Drain is a no-op.
/// </summary>
public class QueuedSynchronizationContext : SynchronizationContext
{
    public override void Post(SendOrPostCallback d, object? state)
    {
        // Updates are synchronous — nothing to queue.
        d(state);
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        d(state);
    }

    /// <summary>
    /// No-op: all updates are now synchronous.
    /// </summary>
    public void Drain()
    {
    }
}

/// <summary>
/// Test helpers for mounting components via <see cref="ReactorDom"/>.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a <see cref="ReactorDom"/>, creates a ComponentNode for
    /// <typeparamref name="T"/> via the type-based factory, mounts it into
    /// a new <see cref="FlexPanel"/>, and returns the component instance,
    /// the <see cref="ReactorDom"/>, and a <see cref="QueuedSynchronizationContext"/>
    /// (for backward compatibility — <c>Drain()</c> is now a no-op).
    /// </summary>
    public static (T component, ReactorDom dom, QueuedSynchronizationContext ctx) MountComponent<T>(params object?[]? args)
        where T : Component
    {
        var container = new FlexPanel();
        var syncCtx = new QueuedSynchronizationContext();
        var dom = new ReactorDom();
        var cnode = ComponentNodeFactory.Create<T>(args);
        dom.Mount(container, cnode);
        syncCtx.Drain();
        var instance = (T)cnode.Instance!;
        return (instance, dom, syncCtx);
    }

    /// <summary>
    /// Creates a <see cref="ReactorDom"/>, mounts the given <paramref name="vnode"/> into
    /// a new <see cref="FlexPanel"/>, and returns the native root, the <see cref="ReactorDom"/>,
    /// and a <see cref="QueuedSynchronizationContext"/> (for backward compatibility).
    /// </summary>
    public static (Visual root, ReactorDom dom, QueuedSynchronizationContext ctx) MountVNode(VNode vnode)
    {
        var container = new FlexPanel();
        var syncCtx = new QueuedSynchronizationContext();
        var dom = new ReactorDom();
        dom.Mount(container, vnode);
        syncCtx.Drain();
        return (dom.Root!, dom, syncCtx);
    }

    /// <summary>
    /// Creates a <see cref="ReactorDom"/> and a <see cref="QueuedSynchronizationContext"/>
    /// for tests that need to manage draining manually (<c>Drain()</c> is now a no-op).
    /// </summary>
    internal static (ReactorDom dom, QueuedSynchronizationContext ctx) CreateDom()
    {
        var ctx = new QueuedSynchronizationContext();
        var dom = new ReactorDom();
        return (dom, ctx);
    }
}
