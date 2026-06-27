using System.Numerics;

namespace NFMWorld.Reactor.Events;

public readonly record struct MouseWheelEvent(
    Vector3 Delta,
    Vector2 Position,
    MouseButtons Buttons,
    bool CtrlKey,
    bool MetaKey,
    bool ShiftKey,
    Vector2 RelativePosition
);