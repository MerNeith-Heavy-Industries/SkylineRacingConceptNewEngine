using System.Collections;
using System.Runtime.CompilerServices;
using Maxine.Extensions.Collections;
using MemoryPack;
using nfm_world_library.Lua;
using NuLua;
using NuLua.Luau;

namespace NFMWorldLibrary.Util;

/// <summary>
/// A lua-compatible T[]
/// </summary>
/// <typeparam name="T">
/// The type of the array's elements. Must implement <see cref="ILuaUserData"/> or be a primitive
/// type for correct functionality.
/// </typeparam>
[LuaShimType("{ [integer|number]: T }")]
[MemoryPackable(GenerateType.Collection)]
public partial class LuaArray<T> : ILuaUserData, IList<T>, IReadOnlyList<T>
{
    public readonly IList<T> Value;

    public LuaArray()
    {
        Value = new List<T>();
    }

    /// <summary>
    /// A lua-compatible T[]
    /// </summary>
    /// <param name="length">The length of the array</param>
    /// <typeparam name="T">
    /// The type of the array's elements. Must implement <see cref="ILuaUserData"/> or be a primitive
    /// type for correct functionality.
    /// </typeparam>
    public LuaArray(int length)
    {
        Value = new T[length];
    }

    public LuaArray(IList<T> innerList)
    {
        Value = innerList;
    }

    public LuaArray(InlineArray2<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray3<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray4<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray5<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray6<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray7<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray8<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray9<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray10<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray11<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray12<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray13<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray14<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray15<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray16<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray2Ex<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray3Ex<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray4Ex<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray5Ex<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray6Ex<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray7Ex<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray8Ex<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray9Ex<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray10Ex<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray11Ex<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray12Ex<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray13Ex<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray14Ex<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray15Ex<T> innerList) => Value = [..innerList];
    public LuaArray(InlineArray16Ex<T> innerList) => Value = [..innerList];

    public T this[int index]
    {
        get => Value[index];
        set => Value[index] = value;
    }
    
    // ------------------------------------------------------------------
    // ILuaUserData — table-like behaviour via metatable
    // ------------------------------------------------------------------

    LuaUserDataMetamethods ILuaUserData.SupportedMetamethods =>
        LuaUserDataMetamethods.Index |
        LuaUserDataMetamethods.NewIndex |
        LuaUserDataMetamethods.Iter |
        LuaUserDataMetamethods.Length;

    bool ILuaUserData.TryGetIndex(LuauState state, LuaValue key, out LuaValue value)
    {
        // Integer key → array index (Lua is 1-indexed)
        if (key.TryRead<double>(out var num) && LuaHelpers.IsLuaIndex(num, out var index))
        {
            if ((uint)index < (uint)Count)
            {
                value = LuaHelpers.ToLuaValue(state, this[index]);
                return true;
            }
        }

        value = default;
        return false;
    }

    bool ILuaUserData.TrySetIndex(LuauState state, LuaValue key, LuaValue value)
    {
        // Integer key → array index (Lua is 1-indexed)
        if (key.TryRead<double>(out var num) && LuaHelpers.IsLuaIndex(num, out var index))
        {
            if (!value.TryRead<T>(out var typedValue))
            {
                // Fallback: try number → T conversion for common numeric types
                typedValue = value.ConvertLuaValue<T>();
            }
            this[index] = typedValue;
            return true;
        }

        return false;
    }
    
    IEnumerator<KeyValuePair<LuaValue, LuaValue>>? ILuaUserData.GetIterator(LuauState state)
    {
        for (var i = 0; i < Value.Count; i++)
        {
            yield return new KeyValuePair<LuaValue, LuaValue>(i + 1, LuaHelpers.ToLuaValue(state, Value[i]));
        }
    }

    long? ILuaUserData.Length => Count;

    public int IndexOf(T item) => Value.IndexOf(item);
    void IList<T>.Insert(int index, T item) => Value.Insert(index, item);
    void IList<T>.RemoveAt(int index) => Value.RemoveAt(index);

    public IEnumerator<T> GetEnumerator() => Value.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    void ICollection<T>.Add(T item) => Value.Add(item);
    void ICollection<T>.Clear() => Value.Clear();
    bool ICollection<T>.Contains(T item) => Value.Contains(item);
    void ICollection<T>.CopyTo(T[] array, int arrayIndex) => Value.CopyTo(array, arrayIndex);
    bool ICollection<T>.Remove(T item) => Value.Remove(item);
    bool ICollection<T>.IsReadOnly => Value.IsReadOnly;

    public int Count => Value.Count;

    public static implicit operator LuaArray<T>(T[] arr) => new(arr);
}