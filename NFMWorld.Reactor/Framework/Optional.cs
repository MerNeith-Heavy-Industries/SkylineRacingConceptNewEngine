using System.Diagnostics.CodeAnalysis;

namespace NFMWorld.Reactor;

/// <summary>
/// A value that may or may not be present. Used for optional parameters
/// in generated factory methods instead of nullable types.
/// </summary>
public readonly struct Optional<T>(T value)
{
    public T? Value { get; } = value;
    
    [MemberNotNullWhen(true, nameof(Value))]
    public bool HasValue { get; } = true;

    public static implicit operator Optional<T>(T value) => new(value);

    public override string ToString() => HasValue ? Value?.ToString() ?? "null" : "<unset>";
}
