using System.Collections;

namespace NFMWorld.Reactor;

/// <summary>
/// A list with value-equality semantics. Two lists are equal if they
/// contain the same elements in the same order. Used for VNode diffing.
/// </summary>
public sealed class EquatableList<T> : IList<T>, IEquatable<EquatableList<T>>
{
    private readonly List<T> _items;

    public EquatableList() => _items = [];
    public EquatableList(IEnumerable<T> items) => _items = [..items];

    public int Count => _items.Count;
    public bool IsReadOnly => false;
    public T this[int index] { get => _items[index]; set => _items[index] = value; }

    public void Add(T item) => _items.Add(item);
    public void AddRange(IEnumerable<T> items) => _items.AddRange(items);
    public void Clear() => _items.Clear();
    public bool Contains(T item) => _items.Contains(item);
    public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
    public int IndexOf(T item) => _items.IndexOf(item);
    public void Insert(int index, T item) => _items.Insert(index, item);
    public bool Remove(T item) => _items.Remove(item);
    public void RemoveAt(int index) => _items.RemoveAt(index);

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    public bool Equals(EquatableList<T>? other)
    {
        if (other is null || other.Count != Count) return false;
        for (int i = 0; i < Count; i++)
            if (!EqualityComparer<T>.Default.Equals(_items[i], other._items[i]))
                return false;
        return true;
    }

    public override bool Equals(object? obj)
        => obj is EquatableList<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in _items)
            hash.Add(item);
        return hash.ToHashCode();
    }

    public static implicit operator EquatableList<T>(T[] items) => new(items);
}
