using System.Numerics;

namespace NFMWorld.Reactor.Events;

public readonly record struct BaseMouseMoveEvent(
    Vector2 Position,
    MouseButtons Buttons,
    bool CtrlKey,
    bool AltKey,
    bool ShiftKey
);