using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using nfm_world_library.Lua;
using NFMWorld.LuaSourceGenerator.Generator.NFMWorld.Library;
using NFMWorldLibrary.FixedMath;
using NuLua;
using NuLua.Luau;

namespace NFMWorldLibrary.Util;

public static class LuaHelpers
{
    public static LuauState OpenState()
    {
        var state = LuauState.CreateSandbox();
        state.OpenLibraries();
        LuaVisibleTypeRegistry.RegisterAll(state);
        state.OpenFixedMathLibrary();

        return state;
    }

    public static LuaValue ToLuaValue<T>(LuauState state, T value)
    {
        if (value is null) return LuaValue.Nil;
        
        if (value is bool @bool)
            return LuaValue.FromBoolean(@bool);
        if (value is float @float)
            return LuaValue.FromNumber(@float);
        if (value is int @int)
            return LuaValue.FromNumber(@int);
        if (value is long @long)
            return LuaValue.FromNumber(@long);
        if (value is uint @uint)
            return LuaValue.FromNumber(@uint);
        if (value is ulong @ulong)
            return LuaValue.FromNumber(@ulong);
        if (value is short @short)
            return LuaValue.FromNumber(@short);
        if (value is ushort @ushort)
            return LuaValue.FromNumber(@ushort);
        if (value is byte @byte)
            return LuaValue.FromNumber(@byte);
        if (value is sbyte @sbyte)
            return LuaValue.FromNumber(@sbyte);
        if (value is double @double)
            return LuaValue.FromNumber(@double);

        if (value is string @string)
            return LuaValue.FromString(@string);

        if (value is LuaFunction func)
            return LuaValue.FromFunction(func);
        if (value is LuaTable table)
            return LuaValue.FromTable(table);
        if (value is ILuaState thread)
            return LuaValue.FromThread(thread);
        
        if (value is fix64 fixed64)
            return LuaValue.FromPrimitive(fixed64);
        if (value is f64Vector3 f64Vector3)
            return LuaValue.FromPrimitive(f64Vector3);
        if (value is f64AngleSingle f64AngleSingle)
            return LuaValue.FromPrimitive(f64AngleSingle);
        if (value is f64Euler f64Euler)
            return LuaValue.FromPrimitive(f64Euler);
        
        if (value is LuaUserData userData)
            return LuaValue.FromUserData(userData);

        if (value is ILuaUserData userData1)
            return state.CreateUserData(userData1);
        
        if (value is Enum)
            return state.CreateEnumUserData(value);

        ThrowInvalidOperationException();
        return default!;
        
        [DoesNotReturn]
        void ThrowInvalidOperationException()
        {
            throw new InvalidOperationException($"ToLuaValue called with wrong type: {value.GetType()}");
        }
    }

    /// <summary>
    /// Checks whether a Lua number represents a valid 1-based array index,
    /// and converts it to a 0-based C# index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLuaIndex(double num, out int csharpIndex)
    {
        // Must be a finite integer ≥ 1 (Lua arrays are 1-indexed)
        if (double.IsFinite(num) && num >= 1.0 && num == Math.Floor(num) && num <= int.MaxValue)
        {
            csharpIndex = (int)num - 1;
            return true;
        }

        csharpIndex = 0;
        return false;
    }

    /// <summary>Converts a <see cref="LuaValue"/> to <typeparamref name="T"/> with flexible coercion.</summary>
    public static T ConvertLuaValue<T>(this LuaValue value)
    {
        if (typeof(T) == typeof(object))
        {
            if (value.TryReadPrimitive<fix64>(out var fixed64)) return (T)(object)fixed64;
            if (value.TryReadPrimitive<f64Vector3>(out var f64Vector3)) return (T)(object)f64Vector3;
            if (value.TryReadPrimitive<f64AngleSingle>(out var f64AngleSingle)) return (T)(object)f64AngleSingle;
            if (value.TryReadPrimitive<f64Euler>(out var f64Euler)) return (T)(object)f64Euler;
        }

        if (typeof(T) == typeof(fix64))
        {
            if (value.TryReadPrimitive<fix64>(out var fixed64)) return (T)(object)fixed64;
            return default!;
        }

        if (typeof(T) == typeof(f64Vector3))
        {
            if (value.TryReadPrimitive<f64Vector3>(out var f64Vector3)) return (T)(object)f64Vector3;
            return default!;
        }

        if (typeof(T) == typeof(f64AngleSingle))
        {
            if (value.TryReadPrimitive<f64AngleSingle>(out var f64AngleSingle)) return (T)(object)f64AngleSingle;
            return default!;
        }

        if (typeof(T) == typeof(f64Euler))
        {
            if (value.TryReadPrimitive<f64Euler>(out var f64Euler)) return (T)(object)f64Euler;
            return default!;
        }

        // Let LuaValue's own conversion handle it (supports double, string, bool, etc.)
        if (value.TryRead<T>(out var result))
            return result;

        ThrowInvalidOperationException();
        return default!;

        void ThrowInvalidOperationException()
        {
            throw new InvalidOperationException($"Cannot convert {value.Type} to {typeof(T).Name}");
        }
    }

    public static bool TryConvertLuaValue<T>(this LuaValue value, out T? outValue)
    {
        if (typeof(T) == typeof(fix64))
        {
            if (value.TryReadPrimitive<fix64>(out var fixed64))
            {
                outValue = (T)(object)fixed64;
                return true;
            }

            outValue = default!;
            return false;
        }
        if (typeof(T) == typeof(f64Vector3))
        {
            if (value.TryReadPrimitive<f64Vector3>(out var fixed64))
            {
                outValue = (T)(object)fixed64;
                return true;
            }

            outValue = default!;
            return false;
        }
        if (typeof(T) == typeof(f64AngleSingle))
        {
            if (value.TryReadPrimitive<f64AngleSingle>(out var fixed64))
            {
                outValue = (T)(object)fixed64;
                return true;
            }

            outValue = default!;
            return false;
        }
        if (typeof(T) == typeof(f64Euler))
        {
            if (value.TryReadPrimitive<f64Euler>(out var fixed64))
            {
                outValue = (T)(object)fixed64;
                return true;
            }

            outValue = default!;
            return false;
        }
        
        // Let LuaValue's own conversion handle it (supports double, string, bool, etc.)
        return value.TryRead(out outValue);
    }
    
    public static LuaTable GamemodeConfigToLuaTable(LuauState state, IReadOnlyDictionary<string, object> dict)
    {
        var table = state.CreateTable();
        foreach (var (k, obj) in dict)
        {
            if (obj is LuaValue val) table[k] = val;
            else if (obj is string str) table[k] = str;
            else if (obj is bool b) table[k] = b;
            else if (obj is byte by) table[k] = by;
            else if (obj is sbyte sby) table[k] = sby;
            else if (obj is short s) table[k] = s;
            else if (obj is ushort u) table[k] = u;
            else if (obj is int i) table[k] = i;
            else if (obj is uint ui) table[k] = ui;
            else if (obj is long l) table[k] = l;
            else if (obj is ulong ul) table[k] = ul;
            else if (obj is float f) table[k] = f;
            else if (obj is double d) table[k] = d;
            else if (obj is fix64 f64) table[k] = LuaValue.FromPrimitive(f64);
        }
        return table;
    }

}