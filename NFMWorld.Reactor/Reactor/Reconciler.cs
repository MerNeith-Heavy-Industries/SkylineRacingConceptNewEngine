using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Maxine.Extensions;
using Maxine.Extensions.Collections;

namespace NFMWorld.Reactor;

/// <summary>
/// Diffs a VNode tree against the native Yoga node tree and applies
/// minimal changes: property updates, child insertion/removal/reorder.
/// </summary>
internal class Reconciler
{
    private class Snapshot
    {
        public BasePropertySnapshot? Previous;
        public BasePropertySnapshot? Current;
    }

    private readonly ThreadLocalObjectPool<Dictionary<object, int>> _dictPool = new(static () => new Dictionary<object, int>(), 50);
    private readonly ThreadLocalArrayPool<Visual?> _visualArrayPool = new(65536, 50);
    private readonly HashSet<Component> _activeComponents = [];
    private readonly HashSet<Component> _visitedComponents = [];
    private readonly List<Visual> _snapshotKeysToRemove = [];
    private readonly Dictionary<Visual, Snapshot> _snapshots = [];
    private readonly Stack<ContextFrame> _contextStack = new();
    private readonly Dictionary<(Visual parent, int childIndex), Component> _componentSlots = [];
    private readonly Queue<Component> _pendingUpdates = new();
    private bool _inBatch;
    private int _batchIterations;
    private const int MaxBatchIterations = 50;

#if DEBUG
    private readonly Stack<ReactorDebugNode> _debugParents = new();
    private readonly List<ReactorDebugNode> _debugRoots = [];
#endif

    private void PushContextFrame() => _contextStack.Push(new ContextFrame());
    internal void PopContextFrame() => _contextStack.Pop();

    internal void SetContext<T>(Context<T> context, T value)
    {
        if (_contextStack.Count == 0) return;
        _contextStack.PeekRef()[context] = value;
    }

    internal T GetContext<T>(Context<T> context)
    {
        foreach (var frame in _contextStack)
        {
            if (frame.TryGetValue(context, out var val) && val is T tval)
                return tval;
        }
        return context.DefaultValue;
    }

    private struct ContextFrame
    {
        private Dictionary<object, object?>? _entries;
        public object? this[Context key]
        {
            set { _entries ??= new Dictionary<object, object?>(); _entries[key] = value; }
        }
        public readonly bool TryGetValue(Context key, out object? value)
        {
            if (_entries is null) { value = null; return false; }
            return _entries.TryGetValue(key, out value);
        }
        public readonly int EntryCount => _entries?.Count ?? 0;
        public readonly void CopyTo(Dictionary<object, object?> destination)
        {
            if (_entries is null) return;
            foreach (var (k, v) in _entries)
                destination[k] = v;
        }
    }

    /// <summary>
    /// Apply the VNode tree to the given native root container.
    /// Returns the native root element (created if needed).
    /// Pass <paramref name="vnode"/> as null to unmount the existing tree
    /// from the container (runs all effect cleanups and <see cref="Component.OnUnmounted"/>).
    /// </summary>
    public Visual? Reconcile(VNode? vnode, Visual container, Visual? existingRoot)
    {
        _inBatch = true;
        _batchIterations = 0;
#if DEBUG
        _debugRoots.Clear();
        _debugParents.Clear();
#endif
        try
        {
            // ── Null vnode: unmount everything ───────────────────────────
            if (vnode is null)
            {
                // Remove existing root from container
                if (existingRoot is not null)
                {
                    if (container.VisualChildren.Count > 0)
                        container.RemoveAt(0);
                }

                // Unmount all active components (runs effect cleanups + OnUnmounted)
                UnmountAllComponents();

                return null;
            }

            var result = ReconcileNode(vnode, existingRoot);
            if (result is null)
                return existingRoot!; // Shouldn't happen for root

            // Ensure the root is a child of the container
            if (container.VisualChildren.Count == 0 || container.VisualChildren[0] != result)
            {
                // If existingRoot is null, the container's current children are stale
                // (e.g. from a previous ReactorDom lifecycle). Clear them all.
                if (container.VisualChildren.Count > 0)
                {
                    if (existingRoot is not null)
                        container.RemoveAt(0);
                    else
                        while (container.VisualChildren.Count > 0)
                            container.RemoveAt(0);
                }
                container.InsertAt(0, result);
            }

            DrainPendingUpdates();
            FinishPass();

#if DEBUG
            NodeDebugger._VDomRootsThisFrame.Clear();
            NodeDebugger._VDomRootsThisFrame.AddRange(_debugRoots);
#endif

            return result;
        }
        finally
        {
            _inBatch = false;
            _batchIterations = 0;
        }
    }

    /// <summary>
    /// Runs post-reconciliation cleanup: unmounts stale components and rotates snapshots.
    /// Called automatically by <see cref="Reconcile"/>; call manually after
    /// <see cref="ReconcileNode"/> when reconciling tree-hosted components via
    /// <see cref="Component.Update"/>.
    /// </summary>
    public void FinishPass()
    {
        UnmountStaleComponents();
        SwapSnapshots();
        _visitedComponents.Clear();
    }

    /// <summary>
    /// Marks a component as visited so <see cref="FinishPass"/> doesn't
    /// treat it as stale. Called by <see cref="Component.Update"/> before
    /// <see cref="FinishPass"/> since tree-hosted updates bypass
    /// <see cref="ReconcileComponentNode"/>.
    /// </summary>
    internal void MarkComponentVisited(Component comp) => _visitedComponents.Add(comp);

    private void UnmountStaleComponents()
    {
        foreach (var comp in _activeComponents)
        {
            if (!_visitedComponents.Contains(comp))
            {
                comp.Unmount();
            }
        }
        _activeComponents.Clear();
        // Swap: visited become active for next pass
        foreach (var comp in _visitedComponents)
            _activeComponents.Add(comp);
    }

    /// <summary>
    /// Unmounts ALL active components unconditionally (runs effect cleanups
    /// and <see cref="Component.OnUnmounted"/>). Used when reconciling a null
    /// VNode to tear down the entire tree.
    /// </summary>
    private void UnmountAllComponents()
    {
        foreach (var comp in _activeComponents)
            comp.Unmount();
        _activeComponents.Clear();
        _visitedComponents.Clear();
        _componentSlots.Clear();
        _snapshots.Clear();
    }

    internal Visual? ReconcileNode(VNode vnode, Visual? existing)
    {
        // ── Component nodes render to their output ───────────────────────
        if (vnode is ComponentNode cnode)
            return ReconcileComponentNode(cnode, existing);

        if (vnode is VisualVNode vvnode)
            return ReconcileVisualNode(existing, vvnode);

        throw new InvalidOperationException("Invalid VNode type: must be VisualVNode or ComponentNode");

    }

    private Visual ReconcileVisualNode(Visual? existing, VisualVNode vvnode)
    {
        // ── Create or reuse native node ──────────────────────────────────
        if (existing is null || existing.GetType() != vvnode.NodeType)
        {
            existing = vvnode.CreateNative();
        }

#if DEBUG
        var debugNode = new ReactorDebugNode(vvnode.NodeType, vvnode.Name, vvnode.Key, existing);
        PushDebugNode(debugNode);
#endif

        // ── Apply properties via the snapshot system ─────────────────────
        ref var snapshot = ref CollectionsMarshal.GetValueRefOrAddDefault(_snapshots, existing, out var exists);
        if (!exists) snapshot = new Snapshot();

        // Restore stale properties from previous pass BEFORE applying current values.
        // prev.AssignProperties resets all properties to their pre-last-pass state;
        // AssignProperties then overwrites only the properties set this pass.
        // Properties not set this pass keep their restored (default) values.
        if (snapshot!.Previous != null)
        {
            snapshot.Previous.AssignProperties(existing);
            snapshot.Previous.ClearProperties();
        }

        vvnode.AssignProperties(existing, ref snapshot!.Current);

        // ── Reconcile children ───────────────────────────────────────────
        if (existing.CanHaveChildren)
        {
            if (vvnode.Children is not null)
                ReconcileChildren(vvnode.Children, existing);
            else
                // ReSharper disable once UseCollectionExpression
                ReconcileChildren(Array.Empty<VNode>(), existing);
        }

#if DEBUG
        PopDebugNode();
#endif

        return existing;
    }

    private void ReconcileChildren(IReadOnlyList<VNode> newChildren, Visual container)
    {
        var existingChildren = container.VisualChildren;

        // ── Keyed reconciliation ─────────────────────────────────────────
        var oldKeyMap = _dictPool.Get();
        for (int i = 0; i < existingChildren.Count; i++)
        {
            var key = GetNodeKey(existingChildren[i]);
            if (key is not null)
                oldKeyMap[key] = i;
        }

        // Determine what goes where
        var pooledArray = _visualArrayPool.Rent(newChildren.Count);
        var newIndexToExisting = pooledArray.AsSpan(0, newChildren.Count);
        for (int i = 0; i < newChildren.Count; i++)
        {
            if (newChildren[i].Key is { } key && oldKeyMap.TryGetValue(key, out var oldIdx))
            {
                TryReuseComponent(newChildren[i], container, oldIdx);
                var reuse = existingChildren[oldIdx];
                var result = ReconcileNode(newChildren[i], reuse);
                SaveComponentSlot(newChildren[i], container, oldIdx);

                // When the node type changes, ReconcileNode creates a new
                // native node. Replace the old one in the container so it
                // doesn't leak stale content.
                if (result != reuse)
                {
                    _snapshots.Remove(reuse);
                    UnmountComponentSubtree(reuse);
                    container.RemoveAt(oldIdx);
                    container.InsertAt(oldIdx, result);
                }

                newIndexToExisting[i] = result;
                oldKeyMap.Remove(key);
            }
            else if (i < existingChildren.Count && !HasKey(existingChildren[i]))
            {
                // Positional match: same index, non-keyed existing child
                TryReuseComponent(newChildren[i], container, i);
                var reuse = existingChildren[i];
                var result = ReconcileNode(newChildren[i], reuse);
                SaveComponentSlot(newChildren[i], container, i);

                // When the node type changes, ReconcileNode creates a new
                // native node. Replace the old one in the container so it
                // doesn't leak stale content.
                if (result != reuse)
                {
                    _snapshots.Remove(reuse);
                    UnmountComponentSubtree(reuse);
                    container.RemoveAt(i);
                    container.InsertAt(i, result);
                }

                newIndexToExisting[i] = result;
            }
            else
            {
                TryReuseComponent(newChildren[i], container, -1);
                newIndexToExisting[i] = ReconcileNode(newChildren[i], null);
                SaveComponentSlot(newChildren[i], container, i);
            }
        }

        // Remove stale keyed children
        foreach (var (_, oldIdx) in oldKeyMap.OrderByDescending(kv => kv.Value))
        {
            var staleChild = existingChildren[oldIdx];
            _snapshots.Remove(staleChild);
            UnmountComponentSlot(container, oldIdx);
            UnmountComponentSubtree(staleChild);
            container.RemoveAt(oldIdx);
        }
        oldKeyMap.Clear();
        _dictPool.Return(oldKeyMap);

        // Apply final ordering
        for (int i = 0; i < newChildren.Count; i++)
        {
            var child = newIndexToExisting[i];
            if (child is null) continue;

            var currentIdx = IndexOfVisual(existingChildren, child);
            if (currentIdx < 0)
            {
                if (i < existingChildren.Count)
                    container.InsertAt(i, child);
                else
                    container.AddChild(child);
            }
            else if (currentIdx != i)
            {
                container.RemoveAt(currentIdx);
                container.InsertAt(i, child);
            }
        }
        _visualArrayPool.Return(pooledArray);

        // Trim excess
        while (existingChildren.Count > newChildren.Count)
        {
            var lastIdx = existingChildren.Count - 1;
            var excessChild = existingChildren[lastIdx];
            _snapshots.Remove(excessChild);
            UnmountComponentSlot(container, lastIdx);
            UnmountComponentSubtree(excessChild);
            container.RemoveAt(lastIdx);
        }
    }

    /// <summary>
    /// Attempts to reuse a component instance from a previous reconciliation
    /// at the given slot position.
    /// </summary>
    private void TryReuseComponent(VNode vnode, Visual container, int childIndex)
    {
        if (vnode is not ComponentNode cnode) return;
        var slot = (container, childIndex);
        if (_componentSlots.TryGetValue(slot, out var existingComp)
            && existingComp.GetType() == cnode.ComponentType)
        {
            // Only reuse the instance if the inputs haven't changed.
            // If inputs changed, the constructor must run again with new values.
            if (existingComp.HasSameInputs(cnode))
            {
                cnode.Instance = existingComp;
            }
            else
            {
                // Inputs changed — the old instance is being replaced.
                // Unmount it so effect cleanups and OnUnmounted run.
                existingComp.Unmount();
                _activeComponents.Remove(existingComp);
                _componentSlots.Remove(slot);
            }
        }
    }

    /// <summary>
    /// Saves a component instance in the slot map for reuse on the next reconciliation pass.
    /// </summary>
    private void SaveComponentSlot(VNode vnode, Visual container, int childIndex)
    {
        if (vnode is ComponentNode { Instance: { } instance })
            _componentSlots[(container, childIndex)] = instance;
    }

    /// <summary>
    /// Unmounts the component (if any) stored in a slot and removes the slot entry.
    /// Called when a child is removed from the native tree during reconciliation.
    /// Also recursively unmounts all descendant component instances in the
    /// removed subtree.
    /// </summary>
    private void UnmountComponentSlot(Visual container, int childIndex)
    {
        var slot = (container, childIndex);
        if (_componentSlots.TryGetValue(slot, out var comp))
        {
            comp.Unmount();
            _activeComponents.Remove(comp);
        }
        _componentSlots.Remove(slot);
    }

    /// <summary>
    /// Recursively unmounts all component instances stored in slots under
    /// <paramref name="removedNode"/> and its descendants. Call when a
    /// native subtree is detached from the visual tree.
    /// </summary>
    internal void UnmountComponentSubtree(Visual removedNode)
    {
        foreach (var child in removedNode.VisualChildren)
            UnmountComponentSubtree(child);

        // Unmount any component slots where this node is the parent container
        var count = removedNode.VisualChildren.Count;
        for (int i = 0; i < count; i++)
            UnmountComponentSlot(removedNode, i);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? GetNodeKey(Visual visual)
        => visual is Node node ? node.Key : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasKey(Visual visual)
        => GetNodeKey(visual) is not null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfVisual(IReadOnlyList<Visual> list, Visual item)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] == item) return i;
        return -1;
    }

    /// <summary>
    /// Reconcile a <see cref="ComponentNode"/> by rendering its component and
    /// reconciling the rendered output against the existing native tree.
    /// </summary>
    private Visual? ReconcileComponentNode(ComponentNode cnode, Visual? existing)
    {
        // Component instance reuse is handled by the caller (ReconcileChildren)
        // via _componentSlots. If not already set, create a new instance.
        if (cnode.Instance is null)
            cnode.Instance = cnode.CreateComponent();

        _visitedComponents.Add(cnode.Instance);

#if DEBUG
        var debugNode = new ReactorDebugNode(cnode.ComponentType, cnode.GetInputs(), existing);
        // The native visual is the component's rendered root — update after RenderViaReconciler.
        PushDebugNode(debugNode);
#endif

        PushContextFrame();
        try
        {
            var result = cnode.Instance.RenderViaReconciler(this, existing, cnode);
#if DEBUG
            debugNode.UpdateNativeVisual(result);
#endif
            return result;
        }
        finally
        {
            PopContextFrame();
#if DEBUG
            PopDebugNode();
#endif
        }
    }

    /// <summary>
    /// Rotates current snapshots → previous for the next pass.
    /// Per-property staleness is handled in <see cref="ReconcileVisualNode"/>
    /// before <see cref="VisualVNode.AssignProperties"/> is called.
    /// </summary>
    private void SwapSnapshots()
    {
        foreach (var (node, snapshots) in _snapshots)
        {
            ref var prev = ref snapshots.Previous;
            ref var current = ref snapshots.Current;

            prev = current;
            current = null;

            if (prev == null)
                _snapshotKeysToRemove.Add(node);
        }
        
        foreach (var visual in _snapshotKeysToRemove)
            _snapshots.Remove(visual);

        _snapshotKeysToRemove.Clear();
    }

    /// <summary>
    /// Removes all snapshot state associated with a native node that is
    /// being detached from the visual tree. Called when a type change
    /// replaces a node so the old one doesn't leak snapshot memory.
    /// </summary>
    internal void RemoveSnapshots(Visual node)
    {
        _snapshots.Remove(node);
    }

    /// <summary>
    /// Enqueues a component for synchronous re-render. If not already in a batch
    /// (i.e., called from outside a <see cref="Reconcile"/> pass), drains the queue
    /// immediately. During a reconciliation pass, updates are collected and drained
    /// at the end by <see cref="Reconcile"/>.
    /// </summary>
    public void EnqueueComponentUpdate(Component comp)
    {
        _pendingUpdates.Enqueue(comp);
        if (!_inBatch)
        {
            _inBatch = true;
            try
            {
                DrainPendingUpdates();
                // Only swap snapshots and merge visited — do NOT run
                // UnmountStaleComponents. Subtree updates must not unmount
                // ancestor/sibling components that weren't visited during
                // this update. Component cleanup for removed children is
                // handled inline by ReconcileChildren.
                SwapSnapshots();
                // Merge newly-visited components into active (subtree-safe:
                // only adds, never removes ancestors/siblings).
                foreach (var visited in _visitedComponents)
                    _activeComponents.Add(visited);
                _visitedComponents.Clear();
            }
            finally
            {
                _inBatch = false;
                _batchIterations = 0;
            }
        }
    }

    /// <summary>
    /// Processes all pending component re-renders synchronously.
    /// Does NOT set or clear <see cref="_inBatch"/> — callers are responsible
    /// for managing the batch flag.
    /// </summary>
    private void DrainPendingUpdates()
    {
        while (_pendingUpdates.TryDequeue(out var comp))
        {
            if (++_batchIterations >= MaxBatchIterations)
                throw new InvalidOperationException(
                    $"Infinite re-render detected: setState calls during Render caused " +
                    $"more than {MaxBatchIterations} consecutive re-renders. " +
                    "Ensure you're not calling setState with a new value on every render.");

            comp.PerformUpdate();
        }
    }

    /// <summary>
    /// Captures a snapshot of the current context stack for replay during
    /// component re-renders. Each frame's entries are copied into a dictionary.
    /// </summary>
    internal List<Dictionary<object, object?>> SnapshotContextStack()
    {
        var snapshots = new List<Dictionary<object, object?>>(_contextStack.Count);
        foreach (var frame in _contextStack)
        {
            var dict = new Dictionary<object, object?>();
            frame.CopyTo(dict);
            snapshots.Add(dict);
        }
        // Reverse so the top of the stack is at the end of the list
        snapshots.Reverse();
        return snapshots;
    }

    /// <summary>
    /// Pushes context frames from a previously captured snapshot onto the stack.
    /// Used by <see cref="Component.PerformUpdate"/> to restore the context that
    /// was visible during the component's initial render.
    /// </summary>
    internal void PushRestoredFrames(List<Dictionary<object, object?>> frames)
    {
        foreach (var frameDict in frames)
        {
            var frame = new ContextFrame();
            foreach (var (key, val) in frameDict)
                frame[(Context)key] = val;
            _contextStack.Push(frame);
        }
    }

#if DEBUG
    private void PushDebugNode(ReactorDebugNode node)
    {
        if (_debugParents.TryPeek(out var parent))
            parent.Children.Add(node);
        else
            _debugRoots.Add(node);
        _debugParents.Push(node);
    }

    private void PopDebugNode()
    {
        _debugParents.Pop();
    }
#endif
}
