using NFMWorld.DriverInterface.DriverInterface;
using NFMWorldLibrary.Util;

namespace NFMWorld.Reactor;

/// <summary>
/// A node containing text. This is a leaf node; it contains no children.
/// </summary>
public class TextNode : Node, IReceivesTextInvalidation, IRichTextElement
{
    public override ReadOnlyLuaArray<Node> VisualChildren => ReadOnlyLuaArray<Node>.Empty;

    public string? Text
    {
        get;
        set
        {
            field = value;
            InvalidateText();
        }
    }

    public void InvalidateText()
    {
        if (VisualParent is IReceivesTextInvalidation invalidation)
        {
            invalidation.InvalidateText();
        }
    }

    Color? IRichTextElement.Background => null;
    Color? IRichTextElement.Foreground => null;
    Color? IRichTextElement.Stroke => null;
    FontFamily? IRichTextElement.FontFamily => null;
    float? IRichTextElement.FontSize => null;
    FontStyle? IRichTextElement.FontStyle => null;
}