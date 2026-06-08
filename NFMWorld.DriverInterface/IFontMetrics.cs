using Microsoft.Xna.Framework;

namespace NFMWorld.DriverInterface;

public interface IFontMetrics
{
    public Vector2 MeasureText(ReadOnlySpan<char> text);
    public float LineHeight { get; }
}