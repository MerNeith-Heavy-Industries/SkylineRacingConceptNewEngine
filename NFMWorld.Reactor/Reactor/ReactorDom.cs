namespace NFMWorld.Reactor;

public class ReactorDom : IDisposable
{
    internal readonly Reconciler Reconciler;
    private VNode? _rootNode;
    private Visual? _container;

    public ReactorDom(SynchronizationContext synchronizationContext)
    {
        Reconciler = new Reconciler(synchronizationContext);
        HotReloadService.UpdateApplicationEvent += OnHotReloadServiceOnUpdateApplicationEvent;
    }

    public void Dispose()
    {
        HotReloadService.UpdateApplicationEvent -= OnHotReloadServiceOnUpdateApplicationEvent;
    }

    private void OnHotReloadServiceOnUpdateApplicationEvent(Type[]? types)
    {
        Update();
    }

    public Visual? Root { get; private set; }

    public void Mount(Visual container, VNode? vnode)
    {
        _container = container;
        _rootNode = vnode;
        Update();
    }

    /// <summary>
    /// Unmounts the current VNode tree from the container, running all
    /// effect cleanups and <see cref="Component.OnUnmounted"/> callbacks.
    /// </summary>
    public void Unmount()
    {
        if (_container != null)
        {
            Root = Reconciler.Reconcile(null, _container, Root);
        }
    }

    private void Update()
    {
        if (_container != null)
        {
            Root = Reconciler.Reconcile(_rootNode, _container, Root);
        }
    }
}