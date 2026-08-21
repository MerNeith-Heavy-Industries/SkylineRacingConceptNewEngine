using System.Numerics;

namespace NFMWorld.Reactor;

public readonly record struct RenderContext(
    Vector2 TopLeft,
    float InheritedOpacity = 1f,
    RectangleF? Clip = null);