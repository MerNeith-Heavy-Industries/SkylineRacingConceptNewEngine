namespace NFMWorld.UI;

public interface IRichTextContainer : IRichTextElement
{
    IReadOnlyList<IRichTextElement> Children { get; }
}