using WorldXaml.UI.Yoga.Events;

namespace NFMWorld.Reactor.Events;

public readonly record struct KeyboardEvent(
    Key KeyChar,
    Key KeyCode,
    Keys Keys
);