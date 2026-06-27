namespace NFMWorld.Reactor.Events;

public readonly record struct NodeEventArgs<T>(T Event, FocusManager FocusManager);