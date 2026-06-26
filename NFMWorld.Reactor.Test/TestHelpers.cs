using WorldXaml.UI.Yoga;

namespace NFMWorld.Reactor.Test;

/// <summary>
/// A SynchronizationContext that queues callbacks posted via <see cref="Post"/>.
/// Call <see cref="Drain"/> to execute all queued work in test-controlled order.
/// </summary>
public class QueuedSynchronizationContext : SynchronizationContext
{
    private readonly Queue<(SendOrPostCallback callback, object? state)> _queue = new();

    public override void Post(SendOrPostCallback d, object? state)
    {
        _queue.Enqueue((d, state));
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        d(state);
    }

    /// <summary>
    /// Executes all queued callbacks and clears the queue.
    /// </summary>
    public void Drain()
    {
        while (_queue.Count > 0)
        {
            var (callback, state) = _queue.Dequeue();
            callback(state);
        }
    }
}

/// <summary>
/// Test helpers for mounting components via <see cref="ReactorDom"/>.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a <see cref="ReactorDom"/> with a queued context,
    /// creates a ComponentNode for <typeparamref name="T"/> via the type-based factory,
    /// mounts it into a new <see cref="FlexPanel"/>,
    /// drains the context, and returns the component instance (from <see cref="ComponentNode.Instance"/>),
    /// the <see cref="ReactorDom"/>, and the <see cref="QueuedSynchronizationContext"/>.
    /// </summary>
    public static (T component, ReactorDom dom, QueuedSynchronizationContext ctx) MountComponent<T>(params object?[]? args)
        where T : Component
    {
        var container = new FlexPanel();
        var syncCtx = new QueuedSynchronizationContext();
        var dom = new ReactorDom(syncCtx);
        var cnode = ComponentNodeFactory.Create<T>(args);
        dom.Mount(container, cnode);
        syncCtx.Drain();
        var instance = (T)cnode.Instance!;
        return (instance, dom, syncCtx);
    }

    /// <summary>
    /// Creates a <see cref="ReactorDom"/> with a queued context,
    /// mounts the given <paramref name="vnode"/> into a new <see cref="FlexPanel"/>,
    /// drains the context, and returns the native root, the <see cref="ReactorDom"/>,
    /// and the <see cref="QueuedSynchronizationContext"/>.
    /// </summary>
    public static (Visual root, ReactorDom dom, QueuedSynchronizationContext ctx) MountVNode(VNode vnode)
    {
        var container = new FlexPanel();
        var syncCtx = new QueuedSynchronizationContext();
        var dom = new ReactorDom(syncCtx);
        dom.Mount(container, vnode);
        syncCtx.Drain();
        return (dom.Root!, dom, syncCtx);
    }

    /// <summary>
    /// Creates a <see cref="ReactorDom"/> with a queued <see cref="QueuedSynchronizationContext"/>
    /// for tests that need to manage draining manually.
    /// </summary>
    internal static (ReactorDom dom, QueuedSynchronizationContext ctx) CreateDom()
    {
        var ctx = new QueuedSynchronizationContext();
        var dom = new ReactorDom(ctx);
        return (dom, ctx);
    }
}