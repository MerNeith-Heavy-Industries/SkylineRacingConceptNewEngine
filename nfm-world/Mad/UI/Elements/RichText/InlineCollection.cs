using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using JetBrains.Annotations;
using NFMWorld.UI;

namespace Avalonia.Controls.Documents;

[WhitespaceSignificantCollection]
public class InlineCollection(ILogical parent) : ObservableCollection<Inline>, IReadOnlyList<IInline>
{
    private IInlineHost? _host = parent as IInlineHost;

    public void AttachHost(IInlineHost? host)
    {
        _host = host;
    }

    IEnumerator<IInline> IEnumerable<IInline>.GetEnumerator()
    {
        return GetEnumerator();
    }

    protected override void InsertItem(int index, Inline item)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        item.LogicalParent = parent;
        item.AttachHost(_host);
        base.InsertItem(index, item);
    }

    protected override void SetItem(int index, Inline item)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        var oldItem = Items[index];
        oldItem.LogicalParent = null;
        oldItem.AttachHost(null);
        item.LogicalParent = parent;
        item.AttachHost(_host);
        base.SetItem(index, item);
    }

    protected override void ClearItems()
    {
        foreach (var node in Items)
        {
            node.LogicalParent = null;
            node.AttachHost(null);
        }
        base.ClearItems();
    }

    protected override void RemoveItem(int index)
    {
        var item = Items[index];
        item.LogicalParent = null;
        item.AttachHost(null);
        base.RemoveItem(index);
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);
        _host?.Invalidate();
    }

    IInline IReadOnlyList<IInline>.this[int index] => this[index];

    [UsedImplicitly]
    public new void Add(Inline inline)
    {
        if (_host is TextRun textBlock && !string.IsNullOrEmpty(textBlock.Text))
        {
            base.Add(new Run(textBlock.Text));

            textBlock.ClearTextInternal();
        }

        base.Add(inline);
    }

    [UsedImplicitly]
    public void Add(string text)
    {
        if (_host is TextRun { HasComplexContent: false } textBlock)
        {
            textBlock.Text += text;
        }
        else
        {
            Add(new Run(text));
        }
    }
}