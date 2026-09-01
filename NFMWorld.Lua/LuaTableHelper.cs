using Lua;

namespace NFMWorld.Lua;

public interface ILuaValueConvertible<out T>
{
    public LuaValue ToLuaValue();
    public static abstract T FromLuaValue(LuaValue table);
}

public static class LuaTableHelper
{
    public static T[] ToArray<T>(LuaTable value) where T : ILuaValueConvertible<T>
    {
        var arr = new T[value.ArrayLength];
        for (var i = 1; i <= value.ArrayLength; i++)
        {
            arr[i - 1] = T.FromLuaValue(value[i]);
        }
        return arr;
    }

    public static LuaTable ArrayToTable<T>(T[] value) where T : ILuaValueConvertible<T>
    {
        var table = new LuaTable(value.Length, 0);
        for (var i = 0; i < value.Length; i++)
        {
            table[i] = value[i].ToLuaValue();
        }
        return table;
    }

    public static TList ToList<T, TList>(LuaTable value) where T : ILuaValueConvertible<T> where TList : IList<T>, new()
    {
        var arr = new TList();
        for (var i = 1; i <= value.ArrayLength; i++)
        {
            arr[i - 1] = T.FromLuaValue(value[i].Read<LuaTable>());
        }
        return arr;
    }
    
    public static LuaTable ListToTable<T, TList>(TList value) where T : ILuaValueConvertible<T> where TList : IList<T>, new()
    {
        var table = new LuaTable(value.Count, 0);
        for (var i = 0; i < value.Count; i++)
        {
            table[i] = value[i].ToLuaValue();
        }
        return table;
    }
}