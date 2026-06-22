using System.Collections;

namespace NFMWorld.Reactor;

/// <summary>
/// A list with value-equality semantics. Two lists are equal if they
/// contain the same elements in the same order. Used for VNode diffing.
/// </summary>
public sealed class EquatableList<T> : List<T>, IEquatable<EquatableList<T>>
{
    public bool Equals(EquatableList<T>? other)
    {
        if (other is null || other.Count != Count) return false;
        for (int i = 0; i < Count; i++)
            if (!EqualityComparer<T>.Default.Equals(this[i], other[i]))
                return false;
        return true;
    }

    public override bool Equals(object? obj)
        => obj is EquatableList<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in this)
            hash.Add(item);
        return hash.ToHashCode();
    }
}
