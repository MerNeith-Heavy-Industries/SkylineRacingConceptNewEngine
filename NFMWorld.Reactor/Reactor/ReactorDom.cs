namespace NFMWorld.Reactor;

public class ReactorDom
{
    private readonly Reconciler _reconciler;
    private VNode? _rootNode;
    private Visual? _container;

    public ReactorDom(SynchronizationContext synchronizationContext)
    {
        _reconciler = new Reconciler(synchronizationContext);
        HotReloadService.UpdateApplicationEvent += OnHotReloadServiceOnUpdateApplicationEvent;
    }

    ~ReactorDom()
    {
        HotReloadService.UpdateApplicationEvent -= OnHotReloadServiceOnUpdateApplicationEvent;
    }

    private void OnHotReloadServiceOnUpdateApplicationEvent(Type[]? types)
    {
        Update();
    }

    public Visual? Root { get; private set; }

    public void Mount(Visual container, VNode vnode)
    {
        _container = container;
        _rootNode = vnode;
        Update();
    }

    private void Update()
    {
        if (_rootNode != null && _container != null)
        {
            Root = _reconciler.Reconcile(_rootNode, _container, Root);
        }
    }
}