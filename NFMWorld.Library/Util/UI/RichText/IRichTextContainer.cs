namespace NFMWorld.Reactor;

public interface IRichTextContainer : IRichTextElement
{
    IReadOnlyList<IRichTextElement> Children { get; }
}