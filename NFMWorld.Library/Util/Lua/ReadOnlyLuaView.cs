using nfm_world_library.Lua;
using NuLua;
using NuLua.Luau;

namespace NFMWorldLibrary.Util;

/// <summary>
/// A lua-compatible list of <see cref="TView"/> backed by a list of <see cref="T"/>
/// </summary>
/// <typeparam name="T">
/// The type of the array's elements. 
/// </typeparam>
/// <typeparam name="TView">
/// The type of the userdata's elements as viewed from Lua. Must implement <see cref="ILuaUserData"/> or be a primitive
/// type for correct functionality.
/// </typeparam>
/// <param name="factory">Converts from T to TView</param>
[LuaShimType("{ [integer|number]: TView }")]
public class ReadOnlyLuaView<T, TView>(IReadOnlyList<T> innerList, Func<T, TView> factory) : ILuaUserData
{
    public readonly IReadOnlyList<T> Value = innerList;

    public T this[int index] => Value[index];

    // ------------------------------------------------------------------
    // ILuaUserData — table-like behaviour via metatable
    // ------------------------------------------------------------------

    LuaUserDataMetamethods ILuaUserData.SupportedMetamethods =>
        LuaUserDataMetamethods.Index |
        LuaUserDataMetamethods.Iter |
        LuaUserDataMetamethods.Length;

    bool ILuaUserData.TryGetIndex(LuauState state, LuaRefValue key, out LuaRefValue value)
    {
        // Integer key → array index (Lua is 1-indexed)
        if (key.TryConvertLuaValue<double>(out var num) && LuaHelpers.IsLuaIndex(num, out var index))
        {
            if ((uint)index < (uint)Value.Count)
            {
                value = LuaHelpers.ToLuaValue(state, LuaProxies.GetOrAdd(this[index]!, factory));
                return true;
            }
        }

        value = default;
        return false;
    }

    IEnumerator<KeyValuePair<LuaRefValue, LuaRefValue>>? ILuaUserData.GetIterator(LuauState state)
    {
        for (var i = 0; i < Value.Count; i++)
        {
            yield return new KeyValuePair<LuaRefValue, LuaRefValue>(i + 1, LuaHelpers.ToLuaValue(state, LuaProxies.GetOrAdd(this[i]!, factory)));
        }
    }

    long? ILuaUserData.Length => Value.Count;
}