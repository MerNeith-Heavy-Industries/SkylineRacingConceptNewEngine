namespace NFMWorld.Reactor;

/// <summary>
/// Base class for user-defined UI components. Subclass and override <see cref="Render"/>.
/// Components can hold state via fields and receive props via constructor.
/// </summary>
public abstract class Component
{
    private Visual? _root;
    private FlexPanel? _container;
    private bool _mounted;

    /// <summary>
    /// The reconciler that applies VNode diffs to the native tree.
    /// </summary>
    public Reconciler Reconciler { get; set; } = new();

    /// <summary>
    /// The native Yoga node that is this component's root in the layout tree.
    /// Created on first render. Null before <see cref="Mount"/>.
    /// </summary>
    public Visual? NativeRoot => _root;

    /// <summary>
    /// The container this component was mounted into.
    /// </summary>
    public FlexPanel? Container => _container;

    /// <summary>
    /// True after the first render has completed.
    /// </summary>
    public bool IsMounted => _mounted;

    /// <summary>
    /// Build the virtual DOM for this component. Override in subclasses.
    /// </summary>
    protected abstract VNode Render();

    /// <summary>
    /// Called after the component is first mounted into the native tree.
    /// </summary>
    protected virtual void OnMounted() { }

    /// <summary>
    /// Called before the component is removed from the native tree.
    /// </summary>
    protected virtual void OnUnmounted() { }

    /// <summary>
    /// Re-render and reconcile into the given container.
    /// </summary>
    public Visual Mount(FlexPanel container)
    {
        _container = container;
        VNode vnode = Render();
        _root = Reconciler.Reconcile(vnode, container, null);
        _mounted = true;
        OnMounted();
        return _root;
    }

    /// <summary>
    /// Render and reconcile as part of a parent <see cref="Reconciler"/> pass
    /// (used when the component is hosted in a <see cref="ComponentNode"/>).
    /// Does not manage container placement — the caller's reconciler handles that.
    /// </summary>
    internal Visual? RenderViaReconciler(Reconciler reconciler, Visual? existing)
    {
        Reconciler = reconciler;
        VNode vnode = Render();
        _root = reconciler.ReconcileNode(vnode, existing);
        if (!_mounted)
        {
            _mounted = true;
            OnMounted();
        }
        return _root;
    }

    /// <summary>
    /// Re-render and reconcile changes into the already-mounted container.
    /// </summary>
    public void Update()
    {
        if (!_mounted || _root is null || _container is null) return;
        VNode vnode = Render();
        _root = Reconciler.Reconcile(vnode, _container, _root);
    }

    /// <summary>
    /// Remove from the native tree.
    /// </summary>
    public void Unmount()
    {
        if (_container is not null && _root is not null)
        {
            _container.Children.Remove(_root);
        }
        _root = null;
        _container = null;
        _mounted = false;
        OnUnmounted();
    }
}
