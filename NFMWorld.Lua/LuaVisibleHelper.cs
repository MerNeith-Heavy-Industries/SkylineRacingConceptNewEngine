using FixedMathSharp;
using Lua;
using NFMWorldLibrary.FixedMath;

namespace NFMWorld.Lua;

/// <summary>
/// Thread-safe registry mapping each <typeparamref name="T"/> to its code-generated metatable.
/// </summary>
public static class LuaVisibleTypeMetatableRegistry<T>
{
    public static LuaTable Metatable => MetatableOrDefault ?? ThrowMetatableNotRegistered();

    // ReSharper disable once StaticMemberInGenericType
    public static LuaTable? MetatableOrDefault
    {
        get;
        private set;
    }

    private static LuaTable ThrowMetatableNotRegistered()
    {
        throw new InvalidOperationException($"Metatable for {typeof(T)} was not registered!");
    }

    public static void Register(LuaTable metatable) => MetatableOrDefault = metatable;
}

/// <summary>
/// Helper for wrapping a value into <see cref="LuaValue"/> using the registered metatable.
/// </summary>
public static class LuaVisibleHelper
{
    /// <summary>
    /// Wraps <paramref name="value"/> into a <see cref="LuaValue"/> if a metatable is registered for <typeparamref name="T"/>.
    /// Throws an exception if no metatable is registered.
    /// </summary>
    public static LuaValue Wrap<T>(T value)
    {
        if (LuaVisibleTypeMetatableRegistry<T>.MetatableOrDefault is { } mt)
            return LuaValue.FromUserData(value, mt);
        return value switch
        {
            null => LuaValue.Nil,
            LuaValue luaValue => luaValue,
            bool boolValue => boolValue,
            double doubleValue => doubleValue,
            string stringValue => stringValue,
            LuaFunction luaFunction => luaFunction,
            LuaTable luaTable => luaTable,
            LuaState luaThread => luaThread,
            ILuaUserData userData => LuaValue.FromUserData(userData),
            int intValue => intValue,
            long longValue => longValue,
            uint uintValue => uintValue,
            ulong ulongValue => ulongValue,
            float floatValue => floatValue,
            Fixed64 fixed64Value => fixed64Value,
            Vector3d vec3Value => vec3Value,
            f64AngleSingle angleValue => new LuaValue(angleValue),
            f64Euler eulerValue => new LuaValue(eulerValue),
            _ => throw new InvalidOperationException(""),
        };
    }

    /// <summary>
    /// Wraps <paramref name="value"/> into a <see cref="LuaValue"/> if a metatable is registered for <typeparamref name="T"/>.
    /// Returns false when no metatable is registered (result is default).
    /// </summary>
    public static bool TryWrap<T>(T value, out LuaValue result)
    {
        if (LuaVisibleTypeMetatableRegistry<T>.Metatable is { } mt)
        {
            result = LuaValue.FromUserData(value, mt);
            return true;
        }

        if (value is null)
        {
            result = LuaValue.Nil;
            return true;
        }

        if (value is LuaValue luaValue)
        {
            result = luaValue;
            return true;
        }

        if (value is bool boolValue)
        {
            result = boolValue;
            return true;
        }

        if (value is double doubleValue)
        {
            result = doubleValue;
            return true;
        }

        if (value is string stringValue)
        {
            result = stringValue;
            return true;
        }

        if (value is LuaFunction luaFunction)
        {
            result = luaFunction;
            return true;
        }

        if (value is LuaTable luaTable)
        {
            result = luaTable;
            return true;
        }

        if (value is LuaState luaThread)
        {
            result = luaThread;
            return true;
        }

        if (value is ILuaUserData userData)
        {
            result = LuaValue.FromUserData(userData);
            return true;
        }

        if (value is int intValue)
        {
            result = intValue;
            return true;
        }

        if (value is long longValue)
        {
            result = longValue;
            return true;
        }

        if (value is uint uintValue)
        {
            result = uintValue;
            return true;
        }

        if (value is ulong ulongValue)
        {
            result = ulongValue;
            return true;
        }

        if (value is float floatValue)
        {
            result = floatValue;
            return true;
        }

        if (value is Fixed64 fixed64Value)
        {
            result = fixed64Value;
            return true;
        }

        if (value is Vector3d vec3Value)
        {
            result = vec3Value;
            return true;
        }

        if (value is f64AngleSingle angleValue)
        {
            result = new LuaValue(angleValue);
            return true;
        }

        if (value is f64Euler eulerValue)
        {
            result = new LuaValue(eulerValue);
            return true;
        }

        result = default;
        return false;
    }
}