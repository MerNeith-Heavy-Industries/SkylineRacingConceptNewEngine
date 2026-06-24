namespace NFMWorld.Reactor;

/// <summary>
/// Non-generic base for <see cref="Context{T}"/>. Used internally by the Reconciler's
/// context scope dictionary and memo version tracking.
/// </summary>
public abstract class Context
{
    private protected Context() { }

    /// <summary>
    /// Monotonically increasing version. Incremented each time
    /// <see cref="Component.ProvideContext{T}"/> sets a new value.
    /// Used by memoized components to detect context changes.
    /// </summary>
    public long Version { get; internal set; }
}

/// <summary>
/// A typed context key with a default value. Use with <see cref="Component.UseContext{T}"/>
/// to read context values from ancestor components, and <see cref="Component.ProvideContext{T}"/>
/// to supply values to descendants.
/// </summary>
public sealed class Context<T>(T defaultValue) : Context
{
    public T DefaultValue { get; } = defaultValue;

    // Identity is reference-based — two Context<T> instances with the same
    // DefaultValue are distinct context keys.
}
