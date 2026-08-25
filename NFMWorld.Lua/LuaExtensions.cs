using FixedMathSharp;
using NFMWorldLibrary.FixedMath;
using NuLua;
using NuLua.Luau;

namespace nfm_world_library.Lua;

public static class LuaExtensions
{
    extension(LuaValue)
    {
        public static LuaValue FromFixed64(Fixed64 value) => LuaValue.FromPrimitive(value);
        public static LuaValue FromFixed64Vector3(Vector3d value) => LuaValue.FromPrimitive(value);
        public static LuaValue FromFixed64Angle(f64AngleSingle value) => LuaValue.FromPrimitive(value);
        public static LuaValue FromFixed64Euler(f64Euler value) => LuaValue.FromPrimitive(value);
    }

    public static void OpenFixedMathLibrary(this LuauState state)
    {
        InitFixed64Support(state);
        InitFixed64Vector3Support(state);
        InitFixed64AngleSupport(state);
        InitFixed64EulerSupport(state);

        state["fixed64"] = state.CreateFunction(static (state, args) =>
        {
            var value = args[0];

            if (value.TryReadPrimitive<Fixed64>(out var fix64))
            {
                state.PushPrimitive(fix64);
                return 1;
            }
            else if (value.TryRead<double>(out var d))
            {
                state.PushPrimitive(Fixed64.CreateFromDouble(d));
                return 1;
            }
            else if (value.TryRead<string>(out var str))
            {
                state.PushPrimitive(Fixed64.Parse(str));
                return 1;
            }

            throw new InvalidOperationException($"Cannot parse argument {value.Type} as fixed64");
        });

        state["fixed64vector3"] = state.CreateFunction(static (state, args) =>
        {
            var x = args[0].ReadPrimitive<Fixed64>();
            var y = args[1].ReadPrimitive<Fixed64>();
            var z = args[2].ReadPrimitive<Fixed64>();

            state.PushPrimitive(new Vector3d(x, y, z));
            return 1;
        });
    }

    private static void InitFixed64EulerSupport(LuauState state)
    {
        using var metatable = state.CreatePrimitiveMetaTable<f64Euler>(f64Euler.PrimitiveId);
        state.SetPrimitiveMetatable<f64Euler>(metatable);
    }

    private static void InitFixed64AngleSupport(LuauState state)
    {
        using var metatable = state.CreatePrimitiveMetaTable<f64AngleSingle>(f64AngleSingle.PrimitiveId);
        state.SetPrimitiveMetatable<f64AngleSingle>(metatable);
    }

    private static void InitFixed64Vector3Support(LuauState state)
    {
        using var metatable = state.CreatePrimitiveMetaTable<Vector3d>(Vector3d.PrimitiveId);
        state.SetPrimitiveMetatable<Vector3d>(metatable);
    }

    private static void InitFixed64Support(LuauState state)
    {
        using var metatable = state.CreatePrimitiveMetaTable<Fixed64>(Fixed64.PrimitiveId);
        state.SetPrimitiveMetatable<Fixed64>(metatable);
    }

}