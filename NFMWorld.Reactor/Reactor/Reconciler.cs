using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Maxine.Extensions;
using Maxine.Extensions.Collections;
using WorldXaml.UI;
using WorldXaml.UI.Base;
using WorldXaml.UI.Yoga;

namespace NFMWorld.Reactor;

/// <summary>
/// Diffs a VNode tree against the native Yoga node tree and applies
/// minimal changes: property updates, child insertion/removal/reorder.
/// </summary>
public class Reconciler
{
    private readonly ThreadLocalObjectPool<Dictionary<object, int>> _dictPool = new(static () => new Dictionary<object, int>(), 50);
    private readonly ThreadLocalArrayPool<Visual?> _visualArrayPool = new(65536, 50);
    private readonly HashSet<Component> _activeComponents = [];
    private readonly HashSet<Component> _visitedComponents = [];
    private readonly Dictionary<Visual, HashSet<int>> _prevPropIds = [];
    private readonly Dictionary<Visual, HashSet<int>> _currPropIds = [];
    private readonly Stack<ContextFrame> _contextStack = new();

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

        SwapPropTracking();

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

        if (vnode is not VisualVNode vvnode)
            throw new InvalidOperationException("Invalid VNode type: must be VisualVNode or ComponentNode");

        // ── Create or reuse native node ──────────────────────────────────
        if (existing is null || existing.GetType() != vvnode.NodeType)
        {
            existing = vvnode.CreateNative();
        }

        // ── Apply properties (Name, and all [Property]-backed values) ──
        if (vvnode.Properties is not null)
        {
            foreach (var (propId, value) in vvnode.Properties)
            {
                var prop = PropertyRegistry.Instance.FindById(propId);
                if (prop is not null && existing is PropertyObject propObject)
                    propObject.SetBoxedValue(prop, value);
            }
        }

        // ── Reset properties that were set in the previous render but not this one ──
        ResetStaleProperties(existing, vvnode);

        // ── Apply VisualVNode direct properties ──────────────────
        if (vvnode.Classes is not null)
        {
            existing.Classes.Clear();
            existing.Classes.AddRange(vvnode.Classes);
        }
        if (vvnode.Name is not null)
            existing.SetValue(Visual.NameProperty, vvnode.Name);

        // ── Reconcile children ───────────────────────────────────────────
        if (vvnode.Children is not null && existing.CanHaveChildren)
        {
            ReconcileChildren(vvnode.Children, existing);
        }

        return existing;
    }

    private void ReconcileChildren(EquatableList<VNode> newChildren, Visual container)
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
                var reuse = existingChildren[oldIdx];
                ReconcileNode(newChildren[i], reuse);
                newIndexToExisting[i] = reuse;
                oldKeyMap.Remove(key);
            }
            else
            {
                newIndexToExisting[i] = ReconcileNode(newChildren[i], null);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? GetNodeKey(Visual visual)
        => visual is Node node ? node.Key : null;

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
        if (cnode.Instance is null)
            cnode.Instance = cnode.CreateComponent();

        _visitedComponents.Add(cnode.Instance);

        PushContextFrame();
        try
        {
            return cnode.Instance.RenderViaReconciler(this, existing);
        }
        finally
        {
            PopContextFrame();
        }
    }

    /// <summary>
    /// Resets any properties that were set on this native node in the previous
    /// reconciliation pass but are absent from the current VNode's Properties.
    /// </summary>
    private void ResetStaleProperties(Visual native, VisualVNode vvnode)
    {
        var currIds = GetOrCreateCurrIds(native);
        currIds.Clear();
        if (vvnode.Properties is not null)
        {
            foreach (var id in vvnode.Properties.Keys)
                currIds.Add(id);
        }

        if (_prevPropIds.TryGetValue(native, out var prevIds))
        {
            foreach (var staleId in prevIds)
            {
                if (currIds.Contains(staleId)) continue;
                var prop = PropertyRegistry.Instance.FindById(staleId);
                if (prop is not null && native is PropertyObject propObject)
                    propObject.SetBoxedValue(prop, prop.DefaultValue);
            }
        }
    }

    /// <summary>
    /// Swaps current→previous property tracking at the end of a reconciliation pass.
    /// </summary>
    internal void SwapPropTracking()
    {
        foreach (var (node, currIds) in _currPropIds)
        {
            if (!_prevPropIds.TryGetValue(node, out var prevIds))
            {
                prevIds = [];
                _prevPropIds[node] = prevIds;
            }
            prevIds.Clear();
            foreach (var id in currIds)
                prevIds.Add(id);
        }
        _currPropIds.Clear();
    }

    private HashSet<int> GetOrCreateCurrIds(Visual node)
    {
        if (!_currPropIds.TryGetValue(node, out var ids))
        {
            ids = [];
            _currPropIds[node] = ids;
        }
        return ids;
    }
}
