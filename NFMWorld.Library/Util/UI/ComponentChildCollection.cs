using WorldXaml.ObservableCollections;

namespace NFMWorld.Reactor;

public class ComponentChildCollection(Component parent) : NonSynchronizedObservableCollection<Node>
{
    protected override void InsertItem(int index, Node item)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);

        // Move semantics (match DOM `insertBefore`): if `item` is already a
        // child, relocate it instead of inserting a duplicate. The Lua React
        // reconciler can re-place a reused host node (e.g. when it detects a
        // move), and without this the host tree grows by one node on every
        // such re-render — the "components multiply on hover/click" bug.
        var existingIndex = Items.IndexOf(item);
        if (existingIndex >= 0)
        {
            if (existingIndex == index)
            {
                return; // already in the right place
            }

            if (existingIndex < index)
            {
                index--;
            }

            RemoveItem(existingIndex);
        }

        if (item is Component cmp)
            parent.NodeInternal.InsertChild(cmp.Contents, (uint)index);
        item.VisualParent = parent;
        base.InsertItem(index, item);
    }

    protected override void SetItem(int index, Node item)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        var oldItem = Items[index];
        oldItem.VisualParent = null;
        if (item is Component cmp)
            parent.NodeInternal.SwapChild(cmp.Contents, (uint)index);
        else if (oldItem is Component cmp1)
            parent.NodeInternal.RemoveChild(cmp1.Contents);
        item.VisualParent = parent;
        base.SetItem(index, item);
    }

    protected override void ClearItems()
    {
        foreach (var item in Items)
        {
            item.VisualParent = null;
        }
        parent.NodeInternal.RemoveAllChildren();
        base.ClearItems();
    }

    protected override void RemoveItem(int index)
    {
        var item = Items[index];
        item.VisualParent = null;
        if (item is Component cmp)
            parent.NodeInternal.RemoveChild(cmp.Contents);
        base.RemoveItem(index);
    }
}