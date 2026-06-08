using Microsoft.Xna.Framework;

namespace NFMWorld.DriverInterface.UI;

public interface IRichTextElement
{
    Color? Background { get; }
    Color? Foreground { get; }
    Color? Stroke { get; }
    FontFamily? FontFamily { get; }
    float? FontSize { get; }
    FontStyle? FontStyle { get; }
}