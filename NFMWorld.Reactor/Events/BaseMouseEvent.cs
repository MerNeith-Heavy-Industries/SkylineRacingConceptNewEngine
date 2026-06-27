using System.Numerics;

namespace NFMWorld.Reactor.Events;

public readonly record struct BaseMouseEvent(
    Vector2 Position,
    MouseButton Button,
    MouseButtons Buttons,
    bool CtrlKey,
    bool AltKey,
    bool ShiftKey
);