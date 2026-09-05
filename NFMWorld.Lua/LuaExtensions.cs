using System.Globalization;
using FixedMathSharp;
using NFMWorldLibrary.FixedMath;
using NuLua;
using NuLua.Luau;

namespace nfm_world_library.Lua;

public static class LuaExtensions
{
    extension(LuaRefValue)
    {
        public static LuaRefValue FromFixed64(Fixed64 value) => LuaRefValue.FromPrimitive(value);
        public static LuaRefValue FromFixed64Vector3(Vector3d value) => LuaRefValue.FromPrimitive(value);
        public static LuaRefValue FromFixed64Angle(f64AngleSingle value) => LuaRefValue.FromPrimitive(value);
        public static LuaRefValue FromFixed64Euler(f64Euler value) => LuaRefValue.FromPrimitive(value);
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
                if (Fixed64.TryParse(str, CultureInfo.InvariantCulture, out var parsed))
                {
                    state.PushPrimitive(parsed);
                    return 1;
                }
                throw new InvalidOperationException($"Cannot parse '{str}' as fixed64");
            }

            throw new InvalidOperationException($"Cannot parse argument {value.Type} as fixed64");
        });

        state["fixed64vector3"] = state.CreateFunction(static (state, args) =>
        {
            var x = ReadFixed64Arg(args[0]);
            var y = ReadFixed64Arg(args[1]);
            var z = ReadFixed64Arg(args[2]);

            state.PushPrimitive(new Vector3d(x, y, z));
            return 1;
        });

        state["f64angle"] = state.CreateFunction(static (state, args) =>
        {
            var value = args[0];

            if (value.TryReadPrimitive<f64AngleSingle>(out var angle))
            {
                state.PushPrimitive(angle);
                return 1;
            }
            else if (value.TryReadPrimitive<Fixed64>(out var f64))
            {
                state.PushPrimitive(f64AngleSingle.FromDegrees(f64));
                return 1;
            }
            else if (value.TryRead<string>(out var str))
            {
                if (Fixed64.TryParse(str, CultureInfo.InvariantCulture, out var degrees))
                {
                    state.PushPrimitive(f64AngleSingle.FromDegrees(degrees));
                    return 1;
                }
                throw new InvalidOperationException($"Cannot parse '{str}' as f64angle");
            }
            else if (value.TryRead<double>(out var d))
            {
                state.PushPrimitive(f64AngleSingle.FromDegrees((Fixed64)d));
                return 1;
            }

            throw new InvalidOperationException($"Cannot parse argument {value.Type} as f64angle");
        });

        state["f64euler"] = state.CreateFunction(static (state, args) =>
        {
            var yaw = ReadAngleArg(args[0]);
            var pitch = ReadAngleArg(args[1]);
            var roll = ReadAngleArg(args[2]);

            state.PushPrimitive(new f64Euler(yaw, pitch, roll));
            return 1;
        });

        state["fixed64vec3"] = LuaRefValue.FromTable(BuildFixed64Vec3Library(state));
        state["f64anglelib"] = LuaRefValue.FromTable(BuildAngleLibrary(state));
        state["f64eulerlib"] = LuaRefValue.FromTable(BuildEulerLibrary(state));
        state["f64math"] = LuaRefValue.FromTable(BuildFixedMathLibrary(state));
    }

    private static Fixed64 ReadFixed64Arg(LuaRefValue value)
    {
        if (value.TryReadPrimitive<Fixed64>(out var f64))
            return f64;
        if (value.TryRead<double>(out var d))
            return Fixed64.CreateFromDouble(d);
        throw new InvalidOperationException($"Cannot convert {value.Type} to Fixed64");
    }

    private static f64AngleSingle ReadAngleArg(LuaRefValue value)
    {
        if (value.TryReadPrimitive<f64AngleSingle>(out var angle))
            return angle;
        if (value.TryRead<double>(out var d))
            return f64AngleSingle.FromDegrees((Fixed64)d);
        if (value.TryReadPrimitive<Fixed64>(out var f64))
            return f64AngleSingle.FromDegrees(f64);
        throw new InvalidOperationException($"Cannot parse argument {value.Type} as f64angle or number");
    }

    private static LuaTableRef BuildFixed64Vec3Library(LuauState state)
    {
        var table = state.CreateTable(0, 12);

        table["normalized"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var v = args[0].ReadPrimitive<Vector3d>();
            state.PushPrimitive(Vector3d.GetNormalized(v));
            return 1;
        }));

        table["cross"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var a = args[0].ReadPrimitive<Vector3d>();
            var b = args[1].ReadPrimitive<Vector3d>();
            state.PushPrimitive(Vector3d.Cross(a, b));
            return 1;
        }));

        table["dot"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var a = args[0].ReadPrimitive<Vector3d>();
            var b = args[1].ReadPrimitive<Vector3d>();
            state.PushPrimitive(Vector3d.Dot(a, b));
            return 1;
        }));

        table["distance"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var a = args[0].ReadPrimitive<Vector3d>();
            var b = args[1].ReadPrimitive<Vector3d>();
            state.PushPrimitive(Vector3d.Distance(a, b));
            return 1;
        }));

        table["sqrdistance"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var a = args[0].ReadPrimitive<Vector3d>();
            var b = args[1].ReadPrimitive<Vector3d>();
            state.PushPrimitive(Vector3d.SqrDistance(a, b));
            return 1;
        }));

        table["magnitude"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var v = args[0].ReadPrimitive<Vector3d>();
            state.PushPrimitive(Vector3d.GetMagnitude(v));
            return 1;
        }));

        table["sqrmagnitude"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var v = args[0].ReadPrimitive<Vector3d>();
            state.PushPrimitive(Vector3d.Dot(v, v));
            return 1;
        }));

        table["max"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var a = args[0].ReadPrimitive<Vector3d>();
            var b = args[1].ReadPrimitive<Vector3d>();
            state.PushPrimitive(Vector3d.Max(a, b));
            return 1;
        }));

        table["min"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var a = args[0].ReadPrimitive<Vector3d>();
            var b = args[1].ReadPrimitive<Vector3d>();
            state.PushPrimitive(Vector3d.Min(a, b));
            return 1;
        }));

        table["lerp"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var a = args[0].ReadPrimitive<Vector3d>();
            var b = args[1].ReadPrimitive<Vector3d>();
            var t = ReadFixed64Arg(args[2]);
            state.PushPrimitive(Vector3d.Lerp(a, b, t));
            return 1;
        }));

        table["abs"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var v = args[0].ReadPrimitive<Vector3d>();
            state.PushPrimitive(Vector3d.Abs(v));
            return 1;
        }));

        table["sign"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var v = args[0].ReadPrimitive<Vector3d>();
            state.PushPrimitive(Vector3d.Sign(v));
            return 1;
        }));

        return table;
    }

    private static LuaTableRef BuildAngleLibrary(LuauState state)
    {
        var table = state.CreateTable(0, 8);

        table["from_radians"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var r = args[0].ReadPrimitive<Fixed64>();
            state.PushPrimitive(f64AngleSingle.FromRadians(r));
            return 1;
        }));

        table["from_degrees"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var d = args[0].ReadPrimitive<Fixed64>();
            state.PushPrimitive(f64AngleSingle.FromDegrees(d));
            return 1;
        }));

        table["wrap"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var a = args[0].ReadPrimitive<f64AngleSingle>();
            state.PushPrimitive(f64AngleSingle.Wrap(a));
            return 1;
        }));

        table["wrap_positive"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var a = args[0].ReadPrimitive<f64AngleSingle>();
            state.PushPrimitive(f64AngleSingle.WrapPositive(a));
            return 1;
        }));

        table["min"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var a = args[0].ReadPrimitive<f64AngleSingle>();
            var b = args[1].ReadPrimitive<f64AngleSingle>();
            state.PushPrimitive(f64AngleSingle.Min(a, b));
            return 1;
        }));

        table["max"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var a = args[0].ReadPrimitive<f64AngleSingle>();
            var b = args[1].ReadPrimitive<f64AngleSingle>();
            state.PushPrimitive(f64AngleSingle.Max(a, b));
            return 1;
        }));

        table["degrees"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var a = args[0].ReadPrimitive<f64AngleSingle>();
            state.PushPrimitive(a.Degrees);
            return 1;
        }));

        table["radians"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var a = args[0].ReadPrimitive<f64AngleSingle>();
            state.PushPrimitive(a.Radians);
            return 1;
        }));

        return table;
    }

    private static LuaTableRef BuildEulerLibrary(LuauState state)
    {
        var table = state.CreateTable(0, 2);

        table["wrap"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var e = args[0].ReadPrimitive<f64Euler>();
            state.PushPrimitive(e.Wrap());
            return 1;
        }));

        table["wrap_positive"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var e = args[0].ReadPrimitive<f64Euler>();
            state.PushPrimitive(e.WrapPositive());
            return 1;
        }));

        return table;
    }

    private static LuaTableRef BuildFixedMathLibrary(LuauState state)
    {
        var table = state.CreateTable(0, 25);

        table["sin"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var x = ReadFixed64Arg(args[0]);
            state.PushPrimitive(FixedMath.Sin(x));
            return 1;
        }));

        table["cos"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var x = ReadFixed64Arg(args[0]);
            state.PushPrimitive(FixedMath.Cos(x));
            return 1;
        }));

        table["tan"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var x = ReadFixed64Arg(args[0]);
            state.PushPrimitive(FixedMath.Tan(x));
            return 1;
        }));

        table["asin"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var x = ReadFixed64Arg(args[0]);
            state.PushPrimitive(FixedMath.Asin(x));
            return 1;
        }));

        table["acos"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var x = ReadFixed64Arg(args[0]);
            state.PushPrimitive(FixedMath.Acos(x));
            return 1;
        }));

        table["atan"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var x = ReadFixed64Arg(args[0]);
            state.PushPrimitive(FixedMath.Atan(x));
            return 1;
        }));

        table["atan2"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var y = ReadFixed64Arg(args[0]);
            var x = ReadFixed64Arg(args[1]);
            state.PushPrimitive(FixedMath.Atan2(y, x));
            return 1;
        }));

        table["sqrt"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var x = ReadFixed64Arg(args[0]);
            state.PushPrimitive(FixedMath.Sqrt(x));
            return 1;
        }));

        table["pow"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var b = ReadFixed64Arg(args[0]);
            var e = ReadFixed64Arg(args[1]);
            state.PushPrimitive(FixedMath.Pow(b, e));
            return 1;
        }));

        table["ln"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var x = ReadFixed64Arg(args[0]);
            state.PushPrimitive(FixedMath.Ln(x));
            return 1;
        }));

        table["log2"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var x = ReadFixed64Arg(args[0]);
            state.PushPrimitive(FixedMath.Log2(x));
            return 1;
        }));

        table["abs"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var x = ReadFixed64Arg(args[0]);
            state.PushPrimitive(FixedMath.Abs(x));
            return 1;
        }));

        table["floor"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var x = ReadFixed64Arg(args[0]);
            state.PushPrimitive(FixedMath.Floor(x));
            return 1;
        }));

        table["ceil"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var x = ReadFixed64Arg(args[0]);
            state.PushPrimitive(FixedMath.Ceiling(x));
            return 1;
        }));

        table["round"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var x = ReadFixed64Arg(args[0]);
            state.PushPrimitive(FixedMath.Round(x));
            return 1;
        }));

        table["min"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var a = ReadFixed64Arg(args[0]);
            var b = ReadFixed64Arg(args[1]);
            state.PushPrimitive(FixedMath.Min(a, b));
            return 1;
        }));

        table["max"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var a = ReadFixed64Arg(args[0]);
            var b = ReadFixed64Arg(args[1]);
            state.PushPrimitive(FixedMath.Max(a, b));
            return 1;
        }));

        table["clamp"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var v = ReadFixed64Arg(args[0]);
            var min = ReadFixed64Arg(args[1]);
            var max = ReadFixed64Arg(args[2]);
            state.PushPrimitive(FixedMath.Clamp(v, min, max));
            return 1;
        }));

        table["clamp01"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var v = ReadFixed64Arg(args[0]);
            state.PushPrimitive(FixedMath.Clamp01(v));
            return 1;
        }));

        table["sign"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var x = ReadFixed64Arg(args[0]);
            state.PushPrimitive((Fixed64)Fixed64.Sign(x));
            return 1;
        }));

        table["lerp"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var a = ReadFixed64Arg(args[0]);
            var b = ReadFixed64Arg(args[1]);
            var t = ReadFixed64Arg(args[2]);
            state.PushPrimitive(Fixed64.Lerp(a, b, t));
            return 1;
        }));

        table["hypot"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var a = ReadFixed64Arg(args[0]);
            var b = ReadFixed64Arg(args[1]);
            state.PushPrimitive(Fixed64.Hypot(a, b));
            return 1;
        }));

        table["deg2rad"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var x = ReadFixed64Arg(args[0]);
            state.PushPrimitive(FixedMath.DegToRad(x));
            return 1;
        }));

        table["rad2deg"] = LuaRefValue.FromFunction(state.CreateFunction(static (state, args) =>
        {
            var x = ReadFixed64Arg(args[0]);
            state.PushPrimitive(FixedMath.RadToDeg(x));
            return 1;
        }));

        table["minValue"] = LuaRefValue.FromPrimitive(Fixed64.MinValue);
        table["maxValue"] = LuaRefValue.FromPrimitive(Fixed64.MaxValue);
        table["pi"] = LuaRefValue.FromPrimitive(FixedMath.PI);
        table["halfpi"] = LuaRefValue.FromPrimitive(FixedMath.PiOver2);
        table["twopi"] = LuaRefValue.FromPrimitive(FixedMath.TwoPI);

        return table;
    }

    private static void InitFixed64EulerSupport(LuauState state)
    {
        using var metatable = state.CreatePrimitiveMetaTable<f64Euler>();
        state.SetPrimitiveMetatable<f64Euler>(metatable);
    }

    private static void InitFixed64AngleSupport(LuauState state)
    {
        using var metatable = state.CreatePrimitiveMetaTable<f64AngleSingle>();
        state.SetPrimitiveMetatable<f64AngleSingle>(metatable);
    }

    private static void InitFixed64Vector3Support(LuauState state)
    {
        using var metatable = state.CreatePrimitiveMetaTable<Vector3d>();
        state.SetPrimitiveMetatable<Vector3d>(metatable);
    }

    private static void InitFixed64Support(LuauState state)
    {
        using var metatable = state.CreatePrimitiveMetaTable<Fixed64>();
        state.SetPrimitiveMetatable<Fixed64>(metatable);
    }

}