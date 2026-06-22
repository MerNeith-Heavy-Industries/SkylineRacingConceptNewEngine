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
    private readonly Dictionary<int, Visual> _keyedInstances = [];

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
        if (container.Children.Count == 0 || container.Children[0] != result)
        {
            if (container.Children.Count > 0 && existingRoot is not null)
                container.Children.Remove(existingRoot);
            container.Children.Insert(0, result);
        }

        return result;
    }

    private Visual? ReconcileNode(VNode vnode, Visual? existing)
    {
        // ── Create or reuse native node ──────────────────────────────────
        if (existing is null || existing.GetType() != vnode.NodeType)
        {
            existing = CreateNative(vnode);
        }

        if (existing is not Node nativeNode)
            return existing;

        // ── Apply properties ─────────────────────────────────────────────
        if (vnode.Properties is not null)
        {
            foreach (var (propId, value) in vnode.Properties)
            {
                var prop = PropertyRegistry.Instance.FindById(propId);
                if (prop is not null && nativeNode is BindableObject bindable)
                    bindable.SetBoxedValue(prop, value);
            }
        }

        // ── Apply classes ────────────────────────────────────────────────
        if (vnode.Classes is not null && nativeNode is BindableObject bo)
        {
            bo.Classes.Clear();
            bo.Classes.AddRange(vnode.Classes);
        }

        // ── Apply name ───────────────────────────────────────────────────
        if (vnode.Name is not null)
            nativeNode.SetValue(BindableObject.NameProperty, vnode.Name);

        // ── Reconcile children ───────────────────────────────────────────
        if (nativeNode is FlexPanel flex && vnode.Children is not null)
        {
            ReconcileChildren(vnode.Children, flex);
        }

        return existing;
    }

    private void ReconcileChildren(EquatableList<VNode> newChildren, FlexPanel container)
    {
        var existingChildren = container.Children;

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
            if (newChildren[i].Key is object key && oldKeyMap.TryGetValue(key, out var oldIdx))
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
            container.Children.RemoveAt(oldIdx);

        // Apply final ordering
        for (int i = 0; i < newChildren.Count; i++)
        {
            var child = newIndexToExisting[i];
            if (child is null) continue;

            var currentIdx = container.Children.IndexOf(child);
            if (currentIdx < 0)
            {
                if (i < container.Children.Count)
                    container.Children.Insert(i, child);
                else
                    container.Children.Add(child);
            }
            else if (currentIdx != i)
            {
                container.Children.RemoveAt(currentIdx);
                container.Children.Insert(i, child);
            }
        }

        // Trim excess
        while (container.Children.Count > newChildren.Count)
            container.Children.RemoveAt(container.Children.Count - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? GetNodeKey(Visual visual)
        => visual is Node node ? node.Key : null;

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
