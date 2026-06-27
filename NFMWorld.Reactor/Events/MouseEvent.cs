using System.Numerics;

namespace NFMWorld.Reactor.Events;

public readonly record struct MouseEvent(
    Vector2 Position,
    MouseButton Button,
    MouseButtons Buttons,
    bool CtrlKey,
    bool MetaKey,
    bool ShiftKey,
    Vector2 RelativePosition
);