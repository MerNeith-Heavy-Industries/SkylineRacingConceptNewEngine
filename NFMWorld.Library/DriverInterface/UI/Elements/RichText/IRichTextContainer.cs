namespace NFMWorld.DriverInterface.UI;

public interface IRichTextContainer : IRichTextElement
{
    IReadOnlyList<IRichTextElement> Children { get; }
}