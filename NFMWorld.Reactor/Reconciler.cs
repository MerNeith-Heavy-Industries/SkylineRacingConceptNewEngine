using System.Runtime.CompilerServices;
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
    /// <summary>
    /// Apply the VNode tree to the given native root container.
    /// Returns the native root element (created if needed).
    /// </summary>
    public Visual Reconcile(VNode vnode, FlexPanel container, Visual? existingRoot)
    {
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

        return result;
    }

    internal Visual? ReconcileNode(VNode vnode, Visual? existing)
    {
        // ── Component nodes render to their output ───────────────────────
        if (vnode is ComponentNode cnode)
            return ReconcileComponentNode(cnode, existing);

        // ── Create or reuse native node ──────────────────────────────────
        if (existing is null || existing.GetType() != vnode.NodeType)
        {
            existing = CreateNative(vnode);
        }

        if (existing is not Node nativeNode)
            return existing;

        // ── Apply properties (Name, Classes, and all [Property]-backed values) ──
        if (vnode.Properties is not null)
        {
            foreach (var (propId, value) in vnode.Properties)
            {
                var prop = PropertyRegistry.Instance.FindById(propId);
                if (prop is not null && nativeNode is BindableObject bindable)
                    bindable.SetBoxedValue(prop, value);
            }
        }

        // ── Apply BindableObjectVNode direct properties ──────────────────
        if (vnode is BindableObjectVNode bvnode)
        {
            if (bvnode.Classes is not null && nativeNode is BindableObject bo)
            {
                bo.Classes.Clear();
                bo.Classes.AddRange(bvnode.Classes);
            }
            if (bvnode.Name is not null)
                nativeNode.SetValue(BindableObject.NameProperty, bvnode.Name);
        }

        // ── Reconcile children ───────────────────────────────────────────
        if (vnode is BindableObjectVNode { Children: not null } bvnodeChildren && nativeNode.CanHaveChildren)
        {
            ReconcileChildren(bvnodeChildren.Children, nativeNode);
        }

        return existing;
    }

    private void ReconcileChildren(EquatableList<VNode> newChildren, Visual container)
    {
        var existingChildren = container.VisualChildren;

        // ── Keyed reconciliation ─────────────────────────────────────────
        var oldKeyMap = new Dictionary<object, int>();
        for (int i = 0; i < existingChildren.Count; i++)
        {
            var key = GetNodeKey(existingChildren[i]);
            if (key is not null)
                oldKeyMap[key] = i;
        }

        // Determine what goes where
        var newIndexToExisting = new Visual?[newChildren.Count];
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
        // Create component instance on first encounter via generated factory
        if (cnode.Instance is null)
        {
            cnode.Instance = cnode.CreateComponent();
        }

        // Let the component render via the reconciler (sets up internal state)
        return cnode.Instance.RenderViaReconciler(this, existing);
    }

    /// <summary>
    /// Instantiate a native Yoga node from a VNode descriptor.
    /// </summary>
    private static Visual CreateNative(VNode vnode)
    {
        var instance = Activator.CreateInstance(vnode.NodeType)
            ?? throw new InvalidOperationException($"Cannot create instance of {vnode.NodeType}");
        return (Visual)instance;
    }
}
