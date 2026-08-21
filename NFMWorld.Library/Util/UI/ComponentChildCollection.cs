using WorldXaml.ObservableCollections;

namespace NFMWorld.Reactor;

public class ComponentChildCollection(Component parent) : NonSynchronizedObservableCollection<Node>
{
    protected override void InsertItem(int index, Node item)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        parent.NodeInternal.InsertChild(item.Contents, (uint)index);
        item.VisualParent = parent;
        base.InsertItem(index, item);
    }

    protected override void SetItem(int index, Node item)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        var oldItem = Items[index];
        oldItem.VisualParent = null;
        parent.NodeInternal.SwapChild(item.Contents, (uint)index);
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
        parent.NodeInternal.RemoveChild(item.Contents);
        base.RemoveItem(index);
    }
}