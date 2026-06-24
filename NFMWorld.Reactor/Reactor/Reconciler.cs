using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Maxine.Extensions;
using Maxine.Extensions.Collections;

namespace NFMWorld.Reactor;

/// <summary>
/// Diffs a VNode tree against the native Yoga node tree and applies
/// minimal changes: property updates, child insertion/removal/reorder.
/// </summary>
public class Reconciler
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

    private void PushContextFrame() => _contextStack.Push(new ContextFrame());
    private void PopContextFrame() => _contextStack.Pop();

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
    }

    /// <summary>
    /// Apply the VNode tree to the given native root container.
    /// Returns the native root element (created if needed).
    /// </summary>
    public Visual Reconcile(VNode vnode, Visual container, Visual? existingRoot)
    {
        _visitedComponents.Clear();

        var result = ReconcileNode(vnode, existingRoot);
        if (result is null)
            return existingRoot!; // Shouldn't happen for root

        // Ensure the root is a child of the container
        if (container.VisualChildren.Count == 0 || container.VisualChildren[0] != result)
        {
            if (container.VisualChildren.Count > 0 && existingRoot is not null)
                container.RemoveAt(0);
            container.InsertAt(0, result);
        }

        // Unmount any components that were active last pass but not visited this pass
        UnmountStaleComponents();

        SwapSnapshots();

        return result;
    }

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

        // ── Apply properties via the snapshot system ─────────────────────
        ref var snapshot = ref CollectionsMarshal.GetValueRefOrAddDefault(_snapshots, existing, out var exists);
        if (!exists) snapshot = new Snapshot();
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
                ReconcileNode(newChildren[i], reuse);
                SaveComponentSlot(newChildren[i], container, oldIdx);
                newIndexToExisting[i] = reuse;
                oldKeyMap.Remove(key);
            }
            else if (i < existingChildren.Count && !HasKey(existingChildren[i]))
            {
                // Positional match: same index, non-keyed existing child
                TryReuseComponent(newChildren[i], container, i);
                var reuse = existingChildren[i];
                ReconcileNode(newChildren[i], reuse);
                SaveComponentSlot(newChildren[i], container, i);
                newIndexToExisting[i] = reuse;
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
            container.RemoveAt(oldIdx);
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
            container.RemoveAt(existingChildren.Count - 1);
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

        PushContextFrame();
        try
        {
            return cnode.Instance.RenderViaReconciler(this, existing, cnode);
        }
        finally
        {
            PopContextFrame();
        }
    }

    /// <summary>
    /// Restores stale properties and swaps current snapshots → previous for the next pass.
    /// </summary>
    private void SwapSnapshots()
    {
        foreach (var (node, snapshots) in _snapshots)
        {
            ref var prev = ref snapshots.Previous;
            ref var current = ref snapshots.Current;
            if (current == null && prev != null)
            {
                // Node not visited this pass — restore its old property values
                prev.AssignProperties(node);
                prev.ClearProperties();
            }

            prev = current;
            current = null;

            if (prev == null)
            {
                _snapshotKeysToRemove.Add(node);
            }
        }
        
        foreach (var visual in _snapshotKeysToRemove)
        {
            _snapshots.Remove(visual);
        }

        _snapshotKeysToRemove.Clear();
    }
}
