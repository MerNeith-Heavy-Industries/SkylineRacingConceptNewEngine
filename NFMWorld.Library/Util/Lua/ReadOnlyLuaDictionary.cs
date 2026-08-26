using System.Collections;
using System.Diagnostics.CodeAnalysis;
using nfm_world_library.Lua;
using NuLua;
using NuLua.Luau;

namespace NFMWorldLibrary.Util;

[LuaShimType("{ [TKey]: TValue }")]
public class ReadOnlyLuaDictionary<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> value)
    : ILuaUserData, IReadOnlyDictionary<TKey, TValue>
{
    public readonly IReadOnlyDictionary<TKey, TValue> Value = value;

    public TValue this[TKey key] => Value[key];

    // ------------------------------------------------------------------
    // ILuaUserData — table-like behaviour via metatable
    // ------------------------------------------------------------------

    LuaUserDataMetamethods ILuaUserData.SupportedMetamethods =>
        LuaUserDataMetamethods.Index |
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

    IEnumerator<KeyValuePair<LuaValue, LuaValue>>? ILuaUserData.GetIterator(LuauState state)
    {
        foreach (var (key, value) in this)
        {
            yield return new KeyValuePair<LuaValue, LuaValue>(LuaHelpers.ToLuaValue(state, key), LuaHelpers.ToLuaValue(state, value));
        }
    }

    long? ILuaUserData.Length => Count;

    public bool ContainsKey(TKey key)
    {
        return Value.ContainsKey(key);
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return Value.TryGetValue(key, out value);
    }

    public IEnumerable<TKey> Keys => Value.Keys;

    public IEnumerable<TValue> Values => Value.Values;

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return Value.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Value).GetEnumerator();
    }

    public int Count => Value.Count;
}