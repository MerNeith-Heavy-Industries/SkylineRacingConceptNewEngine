using Microsoft.Xna.Framework;
using NFMWorld.DriverInterface;

namespace NFMWorld.Reactor;

public interface IRichTextElement
{
    Color? Background { get; }
    Color? Foreground { get; }
    Color? Stroke { get; }
    FontFamily? FontFamily { get; }
    float? FontSize { get; }
    FontStyle? FontStyle { get; }
}