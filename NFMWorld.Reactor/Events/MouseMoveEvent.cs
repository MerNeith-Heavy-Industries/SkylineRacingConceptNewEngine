using System.Numerics;

namespace NFMWorld.Reactor.Events;

public readonly record struct MouseMoveEvent(
    Vector2 Position,
    MouseButtons Buttons,
    bool CtrlKey,
    bool MetaKey,
    bool ShiftKey,
    Vector2 RelativePosition
);