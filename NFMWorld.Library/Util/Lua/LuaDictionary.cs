using System.Collections;
using System.Diagnostics.CodeAnalysis;
using MemoryPack;
using nfm_world_library.Lua;
using NuLua;
using NuLua.Luau;

namespace NFMWorldLibrary.Util;

[LuaShimType("{ [TKey]: TValue }")]
[MemoryPackable(GenerateType.Collection)]
public partial class LuaDictionary<TKey, TValue> : ILuaUserData, IDictionary<TKey, TValue> where TKey : notnull
{
    public readonly IDictionary<TKey, TValue> Value;

    public LuaDictionary()
    {
        Value = new Dictionary<TKey, TValue>();
    }

    public LuaDictionary(IDictionary<TKey, TValue> value)
    {
        Value = value;
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
        if (LuaHelpers.TryConvertLuaValue<TKey>(key, out var typedKey))
        {
            value = LuaHelpers.ToLuaValue(state, this[typedKey]);
            return true;
        }

        value = default;
        return false;
    }

    bool ILuaUserData.TrySetIndex(LuauState state, LuaValue key, LuaValue value)
    {
        if (LuaHelpers.TryConvertLuaValue<TKey>(key, out var typedKey))
        {
            if (LuaHelpers.TryConvertLuaValue<TValue>(value, out var typedValue))
            {
                this[typedKey] = typedValue;
                return true;
            }
        }

        return false;
    }
    
    IEnumerator<KeyValuePair<LuaValue, LuaValue>>? ILuaUserData.GetIterator(LuauState state)
    {
        foreach (var (key, value) in this)
        {
            yield return new KeyValuePair<LuaValue, LuaValue>(LuaHelpers.ToLuaValue(state, key), LuaHelpers.ToLuaValue(state, value));
        }
    }

    long? ILuaUserData.Length => Count;

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return Value.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Value).GetEnumerator();
    }

    public void Add(TKey key, TValue value)
    {
        Value.Add(key, value);
    }

    public bool ContainsKey(TKey key)
    {
        return Value.ContainsKey(key);
    }

    public bool Remove(TKey key)
    {
        return Value.Remove(key);
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return Value.TryGetValue(key, out value);
    }

    public ICollection<TKey> Keys => Value.Keys;

    public ICollection<TValue> Values => Value.Values;

    public TValue this[TKey key]
    {
        get => Value[key];
        set => Value[key] = value;
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        Value.Add(item);
    }

    public void Clear()
    {
        Value.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        return Value.Contains(item);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        Value.CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        return Value.Remove(item);
    }

    public int Count => Value.Count;

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => Value.IsReadOnly;
}