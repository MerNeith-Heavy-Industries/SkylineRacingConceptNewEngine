using System.Collections;
using Avalonia.Controls.Documents;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

namespace NFMWorld.UI;

/// <summary>
/// Span element used for grouping other Inline elements.
/// </summary>
public class Span : Inline, IAddChild<Inline>, IAddChild<string>, IRichTextContainer
{
    /// <summary>
    /// Gets or sets the inlines.
    /// </summary>
    [Content]
    public InlineCollection Inlines { get; }

    private readonly RichTextElementList _subElements;
    IReadOnlyList<IRichTextElement> IRichTextContainer.Children => _subElements;

    public Span()
    {
        Inlines = new InlineCollection(this);
        _subElements = new RichTextElementList(Inlines);
    }

    public override IReadOnlyList<IInline> LogicalChildren => Inlines;

    void IAddChild<Inline>.AddChild(Inline child)
    {
        Inlines.Add(child);
    }

    void IAddChild<string>.AddChild(string child)
    {
        Inlines.Add(new Run(child));
    }

    void IAddChild.AddChild(object child)
    {
        switch (child)
        {
            case Inline inline:
                Inlines.Add(inline);
                break;
            case string str:
                Inlines.Add(new Run(str));
                break;
            default:
                throw new ArgumentException($"Unsupported child type: {child.GetType()}. Expected {nameof(Inline)} or string.");
        }
    }

    public override void AttachHost(IInlineHost? host)
    {
        base.AttachHost(host);
        Inlines.AttachHost(host);
        foreach (var inline in Inlines)
        {
            inline.AttachHost(host);
        }
    }

    private class RichTextElementList(InlineCollection inlines) : IReadOnlyList<IRichTextElement>
    {
        public IEnumerator<IRichTextElement> GetEnumerator()
        {
            foreach (var inline in inlines)
            {
                if (inline is IRichTextElement element)
                {
                    yield return element;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => inlines.Count<ILogical>(inline => inline is IRichTextElement);

        public IRichTextElement this[int index] => inlines.OfType<IRichTextElement>().ElementAt(index);
    }
}