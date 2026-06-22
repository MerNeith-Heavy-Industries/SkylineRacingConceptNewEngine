using System.Collections.ObjectModel;
using WorldXaml.ObservableCollections;

namespace WorldXaml.UI.Yoga;

public class NodeChildCollection(Node parent) : NonSynchronizedObservableCollection<Visual>
{
    protected override void InsertItem(int index, Visual item)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        parent.NodeInternal.InsertChild(item.Contents, (uint)index);
        item.VisualParent = parent;
        base.InsertItem(index, item);
    }

    protected override void SetItem(int index, Visual item)
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