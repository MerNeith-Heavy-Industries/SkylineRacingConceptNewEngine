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
/// <param name="reverseFactory">Converts from TView back to T for writebacks</param>
[LuaShimType("{ [integer|number]: TView }")]
public class LuaView<T, TView>(IList<T> innerList, Func<T, TView> factory, Func<TView, T> reverseFactory) : ILuaUserData
{
    public readonly IList<T> Value = innerList;

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
            if ((uint)index < (uint)Value.Count)
            {
                value = LuaHelpers.ToLuaValue(state, LuaProxies.GetOrAdd(this[index]!, factory));
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
            if (!value.TryRead<TView>(out var typedValue))
            {
                // Fallback: try number → T conversion for common numeric types
                typedValue = value.ConvertLuaValue<TView>();
            }
            this[index] = LuaProxies.GetOrAdd(typedValue, reverseFactory);
            return true;
        }

        return false;
    }
    
    IEnumerator<KeyValuePair<LuaValue, LuaValue>>? ILuaUserData.GetIterator(LuauState state)
    {
        for (var i = 0; i < Value.Count; i++)
        {
            yield return new KeyValuePair<LuaValue, LuaValue>(i + 1, LuaHelpers.ToLuaValue(state, LuaProxies.GetOrAdd(this[i]!, factory)));
        }
    }

    long? ILuaUserData.Length => Value.Count;

}