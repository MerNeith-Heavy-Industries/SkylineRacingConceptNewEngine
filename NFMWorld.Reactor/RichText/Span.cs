namespace NFMWorld.Reactor;

/// <summary>
/// Span element used for grouping other Inline elements.
/// </summary>
public class Span : TextElement, IRichTextContainer
{
    private readonly List<IRichTextElement> _subElements = new();
    IReadOnlyList<IRichTextElement> IRichTextContainer.Children => _subElements;

    public void Add(IRichTextElement child)
    {
        _subElements.Add(child);
    }

    public void Add(string str)
    {
        _subElements.Add(new Run(str));
    }
}