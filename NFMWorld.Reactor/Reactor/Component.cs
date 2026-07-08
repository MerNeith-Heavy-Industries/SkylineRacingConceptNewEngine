using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ObservableCollections;
using WorldXaml.ObservableCollections;

namespace NFMWorld.Reactor;

/// <summary>
/// Base class for user-defined UI components. Subclass and override <see cref="Render"/>.
/// Components can hold state via hooks (<see cref="UseState{T}"/>, <see cref="UseEffect"/>)
/// or fields, and receive props via constructor.
/// </summary>
public abstract class Component
{
    private Visual? _root;
    private bool _mounted;

    #region Hooks infrastructure

    private int _hookIndex;
    private int _prevHookCount;
    private List<Hook?>? _hooks;
    private List<PendingEffect>? _pendingEffects;
    private Action?[]? _cleanupActions;
    private List<Dictionary<object, object?>>? _contextStackSnapshot;

    /// <summary>
    /// The reconciler that applies VNode diffs to the native tree.
    /// </summary>
    internal Reconciler? Reconciler { get; set; }

    /// <summary>
    /// The native Yoga node that is this component's root in the layout tree.
    /// Created on first render. Null before <see cref="RenderViaReconciler"/>.
    /// </summary>
    public Visual? NativeRoot => _root;

    /// <summary>
    /// True after the first render has completed.
    /// </summary>
    public bool IsMounted => _mounted;

    #endregion

    #region Memoization

    private bool _shouldMemo = true;
    private ComponentNode? _lastInputCNode;
    private VNode? _cachedVNode;
    private Dictionary<Context, long>? _contextVersionsRead;
    private Dictionary<Context, long>? _lastContextVersions;
    private long _lastHotReloadGeneration;

    /// <summary>
    /// Disable memoization for this component. When memo is enabled (the default),
    /// the component skips <see cref="Render"/> and reuses the previous VNode tree if:
    /// <list type="number">
    /// <item>Constructor arguments (inputs) haven't changed (reference equality).</item>
    /// <item>Context values read via <see cref="UseContext{T}"/> haven't changed
    /// (tracked by <see cref="Context{T}.Version"/>).</item>
    /// </list>
    /// Call this from the constructor to opt out of memoization.
    /// State changes via <see cref="UseState{T}"/> always trigger a re-render
    /// regardless of memo.
    /// </summary>
    protected void DisableMemo()
    {
        _shouldMemo = false;
    }

    /// <summary>
    /// True if memoization is enabled and inputs haven't changed since the last render.
    /// Checks constructor arguments and context versions.
    /// </summary>
    internal bool ShouldSkipRender(ComponentNode cnode)
    {
        if (!_shouldMemo || _lastInputCNode is null || _cachedVNode is null)
            return false;

        if (!cnode.InputsEqual(_lastInputCNode))
            return false;

        // Check context versions
        if (_lastContextVersions is not null)
        {
            foreach (var (ctx, lastVer) in _lastContextVersions)
            {
                if (ctx.Version != lastVer)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Saves the current inputs and context versions for the next memo check.
    /// Inputs are always saved (needed for instance reuse decisions).
    /// Memo-specific state (cached VNode, context versions) is only saved when memo is enabled.
    /// </summary>
    internal void SaveMemoState(ComponentNode cnode, VNode vnode)
    {
        _lastInputCNode = cnode;
        _lastHotReloadGeneration = HotReloadService.Generation;

        if (!_shouldMemo) return;
        _cachedVNode = vnode;

        if (_contextVersionsRead is not null)
        {
            _lastContextVersions ??= [];
            _lastContextVersions.Clear();
            foreach (var (ctx, ver) in _contextVersionsRead)
                _lastContextVersions[ctx] = ver;
            _contextVersionsRead.Clear();
        }
    }

    /// <summary>
    /// Returns true if the constructor inputs on <paramref name="cnode"/> match
    /// the inputs from this component's last render. Used by the Reconciler
    /// to decide whether to reuse a component instance or create a new one.
    /// </summary>
    internal bool HasSameInputs(ComponentNode cnode)
    {
        if (_lastInputCNode is null) return false;
        return cnode.InputsEqual(_lastInputCNode);
    }

    #endregion

    #region Hooks
    
    /// <summary>
    /// Declare a state variable. Returns the current value and a setter.
    /// Calling the setter schedules a re-render.
    /// </summary>
    protected (T value, Action<Func<T, T>> setValue) UseState<T>(T initialValue, IEqualityComparer<T>? comparer = null)
    {
        VerifyReconciler();

        var box = ValidateHook<StateBox<T>>() ?? AddHook(new StateBox<T>(initialValue, comparer ?? EqualityComparer<T>.Default));
        
        return (box.Value, newValueFactory =>
        {
            var newValue = newValueFactory(box.Value);
            
            if (box.Comparer.Equals(box.Value, newValue)) return;
            box.Value = newValue;
            Update();
        });
    }

    /// <summary>
    /// Run a side effect after the component renders. The returned cleanup
    /// action runs before the next effect invocation or when the component unmounts.
    /// </summary>
    /// <param name="effect">The effect callback. Return an optional cleanup action.</param>
    /// <param name="dependencies">
    /// Optional dependency array. If provided, the effect only re-runs when a dependency
    /// changes (shallow reference equality). Pass <c>null</c> to run on every render.
    /// Pass an empty array to run only on mount and unmount.
    /// </param>
    protected void UseEffect(Func<Action?> effect, params object?[] dependencies)
    {
        VerifyReconciler();

        var box = ValidateHook<DepsBox>() ?? AddHook(new DepsBox(null));

        // Check if dependencies changed since last render
        var hasChanged = !DepsEqual(box.Dependencies, dependencies);
        
        if (hasChanged)
        {
            box.Dependencies = dependencies;
            var hookIdx = _hookIndex - 1; // ValidateHook already incremented _hookIndex
            (_pendingEffects ??= []).Add(new PendingEffect(effect, hookIdx));
        }
    }

    /// <summary>
    /// Run a side effect after the component renders.
    /// </summary>
    /// <param name="effect">The effect callback.</param>
    /// <param name="dependencies">
    /// Optional dependency array. If provided, the effect only re-runs when a dependency
    /// changes (shallow reference equality). Pass <c>null</c> to run on every render.
    /// Pass an empty array to run only on mount and unmount.
    /// </param>
    protected void UseEffect(Action effect, params object?[] dependencies)
    {
        UseEffect(() =>
        {
            effect();
            return null;
        }, dependencies);
    }

    /// <summary>
    /// Memoize a computed value. The factory only re-runs when a dependency changes.
    /// </summary>
    protected T UseMemo<T>(Func<T> factory, params object?[]? dependencies)
    {
        VerifyReconciler();

        var box = ValidateHook<MemoBox<T>>() ?? AddHook(new MemoBox<T>());

        if (box.Dependencies is null || !DepsEqual(box.Dependencies, dependencies))
        {
            box.Value = factory();
            box.Dependencies = dependencies;
        }
        return box.Value;
    }

    /// <summary>
    /// Returns a stable callback that only changes when dependencies change.
    /// </summary>
    protected Action UseCallback(Action callback, params object?[]? dependencies)
        => UseMemo(() => callback, dependencies);

    /// <summary>
    /// Returns a stable callback that only changes when dependencies change.
    /// </summary>
    protected Func<T> UseCallback<T>(Func<T> callback, params object?[]? dependencies)
        => UseMemo(() => callback, dependencies);

    /// <summary>
    /// Returns a stable callback that only changes when dependencies change.
    /// </summary>
    protected Action<T> UseCallback<T>(Action<T> callback, params object?[]? dependencies)
        => UseMemo(() => callback, dependencies);

    /// <summary>
    /// Returns a stable callback that only changes when dependencies change.
    /// </summary>
    protected Action<T1, T2> UseCallback<T1, T2>(Action<T1, T2> callback, params object?[]? dependencies)
        => UseMemo(() => callback, dependencies);

    /// <summary>
    /// Returns a stable callback that only changes when dependencies change.
    /// </summary>
    protected Func<TIn, TOut> UseCallback<TIn, TOut>(Func<TIn, TOut> callback, params object?[]? dependencies)
        => UseMemo(() => callback, dependencies);

    /// <summary>
    /// Returns a mutable ref object that persists across renders.
    /// Mutating <c>ref.Current</c> does NOT trigger a re-render.
    /// </summary>
    protected Ref<T> UseRef<T>(T initialValue = default!)
    {
        VerifyReconciler();

        var box = ValidateHook<StateBox<Ref<T>>>() ?? AddHook(new StateBox<Ref<T>>(new Ref<T>(initialValue), EqualityComparer<Ref<T>>.Default));

        return box.Value;
    }

    /// <summary>
    /// Subscribes to an <see cref="INotifyPropertyChanged"/> source and re-renders
    /// when any property changes. Returns the same source object.
    /// </summary>
    protected T UseObservable<T>(T source) where T : INotifyPropertyChanged
    {
        VerifyReconciler();

        var (tick, setTick) = UseState(0);
        UseEffect(() =>
        {
            source.PropertyChanged += Handler;
            return () => source.PropertyChanged -= Handler;
            void Handler(object? s, PropertyChangedEventArgs e) => setTick(static tick => tick + 1);
        }, source);
        return source;
    }

    /// <summary>
    /// Subscribes to an <see cref="IObservable{T}"/> source and re-renders
    /// when its value changes. Returns the value.
    /// </summary>
    protected T UseObservable<T>(IObservable<T>? source)
    {
        VerifyReconciler();

        var (value, setValue) = UseState(default(T)!);
        UseEffect(() =>
        {
            var subscription = source?.Subscribe(new ReactiveObserver<T>(setValue));
            return subscription != null ? subscription.Dispose : null;
        }, source);
        return value;
    }

    private sealed class ReactiveObserver<T>(Action<Func<T, T>> setValue) : IObserver<T>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(T value)
        {
            setValue(_ => value);
        }
    }

    /// <summary>
    /// Subscribes to a specific property on an <see cref="INotifyPropertyChanged"/> source.
    /// Re-renders only when that property changes.
    /// </summary>
    protected TProp UseObservableProperty<T, TProp>(T source, Func<T, TProp> selector, string propertyName)
        where T : INotifyPropertyChanged
    {
        VerifyReconciler();

        var (tick, setTick) = UseState(0);
        UseEffect(() =>
        {
            source.PropertyChanged += Handler;
            return () => source.PropertyChanged -= Handler;

            void Handler(object? s, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == propertyName || string.IsNullOrEmpty(e.PropertyName))
                    setTick(static tick => tick + 1);
            }
        }, source, propertyName);
        return selector(source);
    }

    /// <summary>
    /// Subscribes to an <see cref="ObservableCollection{T}"/> and re-renders on add/remove/reset.
    /// Returns the collection.
    /// </summary>
    protected ObservableCollection<T> UseCollection<T>(ObservableCollection<T> collection)
    {
        VerifyReconciler();

        var (tick, setTick) = UseState(0);
        UseEffect(() =>
        {
            collection.CollectionChanged += Handler;
            return () => collection.CollectionChanged -= Handler;
            void Handler(object? s, NotifyCollectionChangedEventArgs e) => setTick(static tick => tick + 1);
        }, collection);
        return collection;
    }
    
    /// <summary>
    /// Subscribes to an <see cref="INonSynchronizedObservableCollection{T}"/> and re-renders on add/remove/reset.
    /// Returns the collection.
    /// </summary>
    protected TCollection UseCollection<TCollection, T>(TCollection collection)
        where TCollection : INonSynchronizedObservableCollection<T>
    {
        VerifyReconciler();

        var (tick, setTick) = UseState(0);
        UseEffect(() =>
        {
            collection.CollectionChanged += Handler;
            return () => collection.CollectionChanged -= Handler;
            void Handler(in NotifyCollectionChangedEventArgs<T> notifyCollectionChangedEventArgs) => setTick(static tick => tick + 1);
        }, collection);
        return collection;
    }

    /// <summary>
    /// Reads a context value provided by an ancestor component's
    /// <see cref="ProvideContext{T}"/> call. Returns <see cref="Context{T}.DefaultValue"/>
    /// if no ancestor provides this context.
    /// The value refreshes automatically when any ancestor re-renders — no
    /// subscriptions needed.
    /// </summary>
    protected T UseContext<T>(Context<T> context)
    {
        VerifyReconciler();

        _ = ValidateHook<ContextHook>() ?? AddHook(new ContextHook());
        _hookIndex++;

        // Track context version for memo comparison
        if (_shouldMemo)
        {
            _contextVersionsRead ??= [];
            _contextVersionsRead[context] = context.Version;
        }

        return Reconciler.GetContext(context);
    }

    /// <summary>
    /// Provides a context value to all descendant components in the current subtree.
    /// Call during <see cref="Render"/> before returning the child VNode tree.
    /// </summary>
    protected void ProvideContext<T>(Context<T> context, T value)
    {
        VerifyReconciler();

        context.Version++;
        Reconciler.SetContext(context, value);
    }

    #endregion

    #region Hook lifecycle

    private void BeginRender()
    {
        _prevHookCount = _hookIndex; // save last render's hook count
        _hookIndex = 0;
    }

    /// <summary>
    /// Validates that the hook at the current index matches the expected type
    /// from the previous render. Throws <see cref="HookOrderException"/> on mismatch.
    /// </summary>
    [MemberNotNull(nameof(_hooks))]
    private T? ValidateHook<T>() where T : Hook
    {
        _hooks ??= [];

        var index = _hookIndex++;

        // After first render, check that each hook slot has the same type as before
        if (_prevHookCount > 0)
        {
            if (index >= _prevHookCount)
            {
                throw new HookOrderException(
                    $"Hook #{index} ({typeof(T)}) was called, but only {_prevHookCount} hooks were used in the previous render. " +
                    "Hooks cannot be called conditionally. Ensure hooks are called in the same order every render.");
            }

            if (_hooks[index] is not T hook)
            {
                throw new HookOrderException(
                    $"Hook #{index} expected '{typeof(T)}' but got '{_hooks[index]?.GetType()}'. " +
                    "Hooks must be called in the same order every render. Did you move a hook call or put it inside a condition?");
            }
            
            return hook;
        }

        return null;
    }

    private T AddHook<T>(T hook) where T : Hook
    {
        _hooks ??= [];
        _hooks.Add(hook);
        return hook;
    }

    private void EndRender()
    {
        // Validate hook count matches previous render
        if (_prevHookCount > 0 && _hookIndex != _prevHookCount)
        {
            throw new HookOrderException(
                $"Expected {_prevHookCount} hooks but {_hookIndex} were called. " +
                "Hooks cannot be called conditionally. Ensure every render calls the same number of hooks in the same order.");
        }

        // Run cleanups ONLY for the specific hooks whose effects are re-running
        if (_cleanupActions is not null && _pendingEffects is { Count: > 0 })
        {
            foreach (var pending in _pendingEffects)
            {
                var idx = pending.HookIndex;
                if (idx < _cleanupActions.Length && _cleanupActions[idx] is { } cleanup)
                {
                    cleanup();
                    _cleanupActions[idx] = null;
                }
            }
        }

        // Run new effects, store their cleanups at the corresponding hook index
        if (_pendingEffects is not null)
        {
            _cleanupActions ??= [];
            foreach (var pending in _pendingEffects)
            {
                var cleanup = pending.Effect();
                var idx = pending.HookIndex;
                if (idx >= _cleanupActions.Length)
                    Array.Resize(ref _cleanupActions, idx + 1);
                _cleanupActions[idx] = cleanup;
            }
            _pendingEffects.Clear();
        }
    }

    private void RunUnmountCleanups()
    {
        if (_cleanupActions is null) return;
        for (int i = 0; i < _cleanupActions.Length; i++)
        {
            _cleanupActions[i]?.Invoke();
            _cleanupActions[i] = null;
        }
    }

    /// <summary>
    /// Nulls out stored dependencies for <see cref="DepsBox"/> and
    /// <see cref="MemoBox{T}"/> hooks so that <see cref="UseEffect"/>
    /// and <see cref="UseMemo"/> detect a change and re-execute on the
    /// next render. <see cref="StateBox{T}"/> hooks are left untouched
    /// — component state survives hot reloads.
    /// </summary>
    private void InvalidateStableHooks()
    {
        if (_hooks is null) return;
        foreach (var hook in _hooks)
            hook?.Invalidate();
    }

    #endregion

    #region Lifecycle

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
    /// Render and reconcile as part of a parent <see cref="Reconciler"/> pass
    /// (used when the component is hosted in a <see cref="ComponentNode"/>).
    /// Does not manage container placement — the caller's reconciler handles that.
    /// </summary>
    internal Visual? RenderViaReconciler(Reconciler reconciler, Visual? existing, ComponentNode cnode)
    {
        Reconciler = reconciler;

        // ── Hot reload detection ──────────────────────────────────────
        bool isHotReload = HotReloadService.Generation != _lastHotReloadGeneration;

        // ── Memo check (bypassed on hot reload) ───────────────────────
        if (!isHotReload && ShouldSkipRender(cnode))
            return _cachedVNode is not null ? reconciler.ReconcileNode(_cachedVNode, existing) : null;

        // After a hot reload, invalidate stable hook dependencies so
        // UseEffect / UseMemo re-execute with the updated code.
        if (isHotReload)
            InvalidateStableHooks();

        // Capture context stack for replay during deferred re-renders
        _contextStackSnapshot = reconciler.SnapshotContextStack();

        BeginRender();
        VNode vnode = Render();
        EndRender();
        SaveMemoState(cnode, vnode);

        _root = reconciler.ReconcileNode(vnode, existing);
        if (!_mounted)
        {
            _mounted = true;
            OnMounted();
        }
        return _root;
    }

    /// <summary>
    /// Enqueues a synchronous re-render. Called by hooks (<see cref="UseState{T}"/> setter, etc.)
    /// when state changes. During a <see cref="Reconciler.Reconcile"/> pass, updates are batched
    /// and drained at the end. Outside a pass, the drain happens immediately.
    /// </summary>
    internal void Update()
    {
        VerifyReconciler();
        Reconciler.EnqueueComponentUpdate(this);
    }

    /// <summary>
    /// Performs a synchronous re-render of this component. Called by the
    /// Reconciler's work loop during <see cref="Reconciler.DrainPendingUpdates"/>.
    /// Restores the context stack snapshot captured during the initial render
    /// so that <see cref="UseContext{T}"/> works correctly during re-renders.
    /// </summary>
    internal void PerformUpdate()
    {
        if (!_mounted || _root is null) return;

        // After a hot reload, invalidate stable hook dependencies so
        // UseEffect / UseMemo re-execute with the updated code.
        if (HotReloadService.Generation != _lastHotReloadGeneration)
            InvalidateStableHooks();

        RestoreContextFrames();
        try
        {
            BeginRender();
            VNode vnode = Render();
            EndRender();

            // Save memo state so ShouldSkipRender uses the latest VNode tree
            // on future parent-triggered reconciliation passes.
            // _lastInputCNode was set during the initial render and remains valid
            // since inputs haven't changed (this is a state-triggered update).
            if (_lastInputCNode is not null)
                SaveMemoState(_lastInputCNode, vnode);

            var oldRoot = _root;
            _root = Reconciler.ReconcileNode(vnode, oldRoot);

            // When the rendered root type changes, ReconcileNode creates a
            // new native node. The old one is still in the parent's children
            // list — replace it via VisualParent so the ordering phase's
            // work isn't silently invalidated.
            if (_root != oldRoot)
            {
                var parent = oldRoot.VisualParent;
                if (parent != null)
                {
                    var children = parent.VisualChildren;
                    for (int i = 0; i < children.Count; i++)
                    {
                        if (children[i] == oldRoot)
                        {
                            parent.RemoveAt(i);
                            parent.InsertAt(i, _root);
                            break;
                        }
                    }
                }

                // Clean up all component instances in the orphaned subtree
                Reconciler.UnmountComponentSubtree(oldRoot);
                Reconciler.RemoveSnapshots(oldRoot);
            }

            Reconciler.MarkComponentVisited(this);
        }
        finally
        {
            ClearContextFrames();
        }
    }

    private void RestoreContextFrames()
    {
        if (_contextStackSnapshot is { Count: > 0 })
            Reconciler!.PushRestoredFrames(_contextStackSnapshot);
    }

    private void ClearContextFrames()
    {
        if (_contextStackSnapshot is not null)
        {
            for (int i = 0; i < _contextStackSnapshot.Count; i++)
                Reconciler!.PopContextFrame();
        }
    }

    /// <summary>
    /// Remove from the native tree. Runs effect cleanups and <see cref="OnUnmounted"/>.
    /// </summary>
    internal void Unmount()
    {
        RunUnmountCleanups();
        _root = null;
        _mounted = false;
        _hookIndex = 0;
        _prevHookCount = 0;
        _hooks?.Clear();
        OnUnmounted();
    }
    
    #endregion

    #region Internal types

    private sealed record PendingEffect(Func<Action?> Effect, int HookIndex);

    private class Hook
    {
        /// <summary>
        /// Called after a hot reload to clear stable dependency state,
        /// forcing <see cref="UseEffect"/> and <see cref="UseMemo"/> to
        /// re-execute on the next render. <see cref="StateBox{T}"/> does
        /// NOT override this — state is preserved across hot reloads.
        /// </summary>
        public virtual void Invalidate() { }
    }

    private sealed class StateBox<T>(T value, IEqualityComparer<T> comparer) : Hook
    {
        public T Value = value;
        public readonly IEqualityComparer<T> Comparer = comparer;
    }

    private sealed class DepsBox(object?[]? dependencies) : Hook
    {
        public object?[]? Dependencies = dependencies;

        public override void Invalidate()
        {
            if (Dependencies is not null)
                Dependencies = null;
        }
    }

    private sealed class ContextHook : Hook;

    private sealed class MemoBox<T> : Hook
    {
        public T Value = default!;
        public object?[]? Dependencies;

        public override void Invalidate()
        {
            if (Dependencies is not null)
                Dependencies = null;
        }
    }

    private static bool DepsEqual(object?[]? a, object?[]? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (!EqualityComparer<object>.Default.Equals(a[i], b[i]))
                return false;
        return true;
    }
    
    #endregion
    
    [MemberNotNull(nameof(Reconciler))]
    private void VerifyReconciler([CallerMemberName] string method = "")
    {
        if (Reconciler is null)
            throw new InvalidOperationException($"{method} can only be called during Render when the component is hosted in a Reconciler.");
    }

}
