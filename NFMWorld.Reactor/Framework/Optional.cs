using System.Diagnostics.CodeAnalysis;

namespace NFMWorld.Reactor;

public readonly struct Optional<T>(T value)
{
    public T? Value { get; } = value;

    [MemberNotNullWhen(true, nameof(Value))]
    public bool HasValue { get; } = true;
    
    public static implicit operator Optional<T>(T value) => new(value);
}