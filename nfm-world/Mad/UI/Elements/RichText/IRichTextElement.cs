using NFMWorld.Util;

namespace NFMWorld.UI;

public interface IRichTextElement
{
    Color? Background { get; }
    Color? Foreground { get; }
    Color? Stroke { get; }
    FontFamily? FontFamily { get; }
    float? FontSize { get; }
    FontStyle? FontStyle { get; }
}