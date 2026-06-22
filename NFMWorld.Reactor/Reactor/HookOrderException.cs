namespace NFMWorld.Reactor;

/// <summary>
/// Thrown when hooks are called in a different order or count between renders,
/// or when a hook is called conditionally. Hooks must be called in the same
/// order on every render.
/// </summary>
public class HookOrderException(string message) : InvalidOperationException(message);
