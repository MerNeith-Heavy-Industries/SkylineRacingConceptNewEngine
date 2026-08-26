using FixedMathSharp;
using NFMWorldLibrary.FixedMath;
using NuLua;
using NuLua.Luau;
using nfm_world_library.Lua;

namespace NFMWorld.LuaSourceGenerator.Test;

/// <summary>
/// NuLua port of the Lua-CSharp FixedMathLibrary test suite (see
/// <c>Lua-CSharp/tests/Lua.Tests/FixedMathLibraryTests.cs</c>).
///
/// NuLua-specific notes vs the Lua-CSharp original:
///  - All fixed types (fixed64 / fixed64vector3 / f64angle / f64euler) marshal to the single
///    <see cref="LuaValueType.Primitive"/> type; the concrete type is recovered via
///    <c>TryReadPrimitive&lt;T&gt;</c>.
///  - The primitive metatable dispatch requires same-typed operands for arithmetic (both operands must
///    share the primitive id), so cross-type operations (e.g. <c>vector * fixed64</c>,
///    <c>f64euler * f64angle</c>, <c>fixed64 &lt; number</c>) are NOT supported — those tests are omitted.
///  - <c>type()</c> name semantics differ (Luau reports "Fixed64" etc.), so the <c>type()</c> tests are
///    omitted.
/// </summary>
[TestClass]
public class FixedMathLibraryTests
{
    private LuauState CreateState()
    {
        var state = LuauState.Create();
        state.OpenLibraries();
        state.OpenFixedMathLibrary();
        return state;
    }

    private static Fixed64 ReadFixed64(LuaValue v)
    {
        Assert.IsTrue(v.TryReadPrimitive<Fixed64>(out var r), "expected Fixed64 primitive");
        return r;
    }

    private static Vector3d ReadVec3(LuaValue v)
    {
        Assert.IsTrue(v.TryReadPrimitive<Vector3d>(out var r), "expected Vector3d primitive");
        return r;
    }

    private static f64AngleSingle ReadAngle(LuaValue v)
    {
        Assert.IsTrue(v.TryReadPrimitive<f64AngleSingle>(out var r), "expected f64AngleSingle primitive");
        return r;
    }

    private static f64Euler ReadEuler(LuaValue v)
    {
        Assert.IsTrue(v.TryReadPrimitive<f64Euler>(out var r), "expected f64Euler primitive");
        return r;
    }

    // ---- fixed64(value) constructor ----

    [TestMethod]
    public void Fixed64Constructor_FromNumber()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64(3.5)");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        Assert.AreEqual((Fixed64)3.5, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void Fixed64Constructor_FromString()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64('2.5')");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        Assert.AreEqual((Fixed64)2.5, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void Fixed64Constructor_FromFixed64_ReturnsSame()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64(fixed64(4.0))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        Assert.AreEqual((Fixed64)4, ReadFixed64(results[0]));
    }

    // ---- fixed64vector3(x, y, z) constructor ----

    [TestMethod]
    public void Fixed64Vector3Constructor_FromNumbers()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64vector3(1, 2, 3)");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        Assert.AreEqual(new Vector3d(1, 2, 3), ReadVec3(results[0]));
    }

    [TestMethod]
    public void Fixed64Vector3Constructor_FromFixed64()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64vector3(fixed64(4), fixed64(5), fixed64(6))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        Assert.AreEqual(new Vector3d(4, 5, 6), ReadVec3(results[0]));
    }

    // ---- Fixed64 arithmetic (same-type only) ----

    [TestMethod]
    public void Fixed64_Add_SameType()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64(2) + fixed64(3)");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual((Fixed64)5, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void Fixed64_Sub_SameType()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64(10) - fixed64(3)");

        Assert.AreEqual((Fixed64)7, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void Fixed64_Mul_SameType()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64(4) * fixed64(2.5)");

        Assert.AreEqual((Fixed64)10, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void Fixed64_Div_SameType()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64(10) / fixed64(4)");

        Assert.AreEqual((Fixed64)2.5, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void Fixed64_Unm()
    {
        using var state = CreateState();
        var results = state.DoString("return -fixed64(5)");

        Assert.AreEqual((Fixed64)(-5), ReadFixed64(results[0]));
    }

    // ---- Fixed64 comparison (same-type only) ----

    [TestMethod]
    public void Fixed64_LessThan()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64(3) < fixed64(5)");

        Assert.AreEqual(1, results.Length);
        Assert.IsTrue(results[0].Read<bool>());
    }

    [TestMethod]
    public void Fixed64_LessThanOrEqual()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64(5) <= fixed64(5)");

        Assert.AreEqual(1, results.Length);
        Assert.IsTrue(results[0].Read<bool>());
    }

    // ---- Fixed64Vector3 arithmetic (same-type only) ----

    [TestMethod]
    public void Fixed64Vector3_Add()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64vector3(1, 2, 3) + fixed64vector3(4, 5, 6)");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(new Vector3d(5, 7, 9), ReadVec3(results[0]));
    }

    [TestMethod]
    public void Fixed64Vector3_Sub()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64vector3(4, 5, 6) - fixed64vector3(1, 2, 3)");

        Assert.AreEqual(new Vector3d(3, 3, 3), ReadVec3(results[0]));
    }

    [TestMethod]
    public void Fixed64Vector3_ComponentWiseMul()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64vector3(1, 2, 3) * fixed64vector3(4, 5, 6)");

        Assert.AreEqual(new Vector3d(4, 10, 18), ReadVec3(results[0]));
    }

    [TestMethod]
    public void Fixed64Vector3_Unm()
    {
        using var state = CreateState();
        var results = state.DoString("return -fixed64vector3(1, 2, 3)");

        Assert.AreEqual(new Vector3d(-1, -2, -3), ReadVec3(results[0]));
    }

    // ---- fixed64vec3.* math functions ----

    [TestMethod]
    public void Fixed64Vec3_Normalized()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64vec3.normalized(fixed64vector3(3, 0, 0))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(new Vector3d(1, 0, 0), ReadVec3(results[0]));
    }

    [TestMethod]
    public void Fixed64Vec3_Cross()
    {
        using var state = CreateState();
        var results = state.DoString(
            "local a = fixed64vector3(1, 0, 0); local b = fixed64vector3(0, 1, 0); return fixed64vec3.cross(a, b)");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(new Vector3d(0, 0, 1), ReadVec3(results[0]));
    }

    [TestMethod]
    public void Fixed64Vec3_Dot()
    {
        using var state = CreateState();
        var results = state.DoString(
            "local a = fixed64vector3(1, 0, 0); local b = fixed64vector3(1, 0, 0); return fixed64vec3.dot(a, b)");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(Fixed64.One, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void Fixed64Vec3_Dot_Perpendicular_Zero()
    {
        using var state = CreateState();
        var results = state.DoString(
            "local a = fixed64vector3(1, 0, 0); local b = fixed64vector3(0, 1, 0); return fixed64vec3.dot(a, b)");

        Assert.AreEqual(Fixed64.Zero, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void Fixed64Vec3_Distance()
    {
        using var state = CreateState();
        var results = state.DoString(
            "local a = fixed64vector3(0, 0, 0); local b = fixed64vector3(3, 4, 0); return fixed64vec3.distance(a, b)");

        Assert.AreEqual((Fixed64)5, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void Fixed64Vec3_Magnitude()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64vec3.magnitude(fixed64vector3(0, 3, 4))");

        Assert.AreEqual((Fixed64)5, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void Fixed64Vec3_SqrMagnitude()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64vec3.sqrmagnitude(fixed64vector3(1, 2, 3))");

        Assert.AreEqual((Fixed64)14, ReadFixed64(results[0])); // 1+4+9=14
    }

    [TestMethod]
    public void Fixed64Vec3_SqrDistance()
    {
        using var state = CreateState();
        var results = state.DoString(
            "return fixed64vec3.sqrdistance(fixed64vector3(0, 0, 0), fixed64vector3(3, 4, 0))");

        Assert.AreEqual((Fixed64)25, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void Fixed64Vec3_Max()
    {
        using var state = CreateState();
        var results = state.DoString(
            "return fixed64vec3.max(fixed64vector3(1, 5, 3), fixed64vector3(4, 2, 6))");

        Assert.AreEqual(new Vector3d(4, 5, 6), ReadVec3(results[0]));
    }

    [TestMethod]
    public void Fixed64Vec3_Min()
    {
        using var state = CreateState();
        var results = state.DoString(
            "return fixed64vec3.min(fixed64vector3(1, 5, 3), fixed64vector3(4, 2, 6))");

        Assert.AreEqual(new Vector3d(1, 2, 3), ReadVec3(results[0]));
    }

    [TestMethod]
    public void Fixed64Vec3_Lerp()
    {
        using var state = CreateState();
        var results = state.DoString(
            "return fixed64vec3.lerp(fixed64vector3(0, 0, 0), fixed64vector3(10, 10, 10), fixed64(0.5))");

        Assert.AreEqual(new Vector3d(5, 5, 5), ReadVec3(results[0]));
    }

    [TestMethod]
    public void Fixed64Vec3_Abs()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64vec3.abs(fixed64vector3(-1, 2, -3))");

        Assert.AreEqual(new Vector3d(1, 2, 3), ReadVec3(results[0]));
    }

    [TestMethod]
    public void Fixed64Vec3_Sign()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64vec3.sign(fixed64vector3(-2, 0, 3))");

        Assert.AreEqual(new Vector3d(-1, 0, 1), ReadVec3(results[0]));
    }

    // ---- f64angle constructor ----

    [TestMethod]
    public void f64Angle_FromNumber()
    {
        using var state = CreateState();
        var results = state.DoString("return f64angle(90)");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        Assert.AreEqual((Fixed64)90, ReadAngle(results[0]).Degrees);
    }

    [TestMethod]
    public void f64Angle_FromFixed64()
    {
        using var state = CreateState();
        var results = state.DoString("return f64angle(fixed64(180))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual((Fixed64)180, ReadAngle(results[0]).Degrees);
    }

    [TestMethod]
    public void f64Angle_FromString()
    {
        using var state = CreateState();
        var results = state.DoString("return f64angle('180')");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
    }

    // ---- f64euler constructor ----

    [TestMethod]
    public void f64Euler_FromAngles()
    {
        using var state = CreateState();
        var results = state.DoString("return f64euler(f64angle(0), f64angle(90), f64angle(0))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        var e = ReadEuler(results[0]);
        Assert.AreEqual((Fixed64)0, e.Yaw.Degrees);
        Assert.AreEqual((Fixed64)90, e.Pitch.Degrees);
        Assert.AreEqual((Fixed64)0, e.Roll.Degrees);
    }

    [TestMethod]
    public void f64Euler_FromNumbers()
    {
        using var state = CreateState();
        var results = state.DoString("return f64euler(45, 0, 0)");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        Assert.AreEqual((Fixed64)45, ReadEuler(results[0]).Yaw.Degrees);
    }

    // ---- f64Euler arithmetic (same-type only) ----

    [TestMethod]
    public void f64Euler_Add()
    {
        using var state = CreateState();
        var results = state.DoString("return f64euler(45, 0, 0) + f64euler(45, 0, 0)");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        ReadEuler(results[0]);
    }

    [TestMethod]
    public void f64Euler_Sub()
    {
        using var state = CreateState();
        var results = state.DoString("return f64euler(90, 0, 0) - f64euler(45, 0, 0)");

        Assert.AreEqual(1, results.Length);
        ReadEuler(results[0]);
    }

    [TestMethod]
    public void f64Euler_Unm()
    {
        using var state = CreateState();
        var results = state.DoString("return -f64euler(45, 30, 15)");

        Assert.AreEqual(1, results.Length);
        ReadEuler(results[0]);
    }

    // ---- f64AngleSingle arithmetic (same-type only) ----

    [TestMethod]
    public void f64AngleSingle_Add()
    {
        using var state = CreateState();
        var results = state.DoString("return f64angle(30) + f64angle(60)");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        ReadAngle(results[0]);
    }

    [TestMethod]
    public void f64AngleSingle_Sub()
    {
        using var state = CreateState();
        var results = state.DoString("return f64angle(90) - f64angle(30)");

        Assert.AreEqual(1, results.Length);
        ReadAngle(results[0]);
    }

    [TestMethod]
    public void f64AngleSingle_Unm()
    {
        using var state = CreateState();
        var results = state.DoString("return -f64angle(45)");

        Assert.AreEqual(1, results.Length);
        ReadAngle(results[0]);
    }

    // ---- f64anglelib functions ----

    [TestMethod]
    public void f64AngleLib_FromRadians()
    {
        using var state = CreateState();
        var results = state.DoString("return f64anglelib.from_radians(fixed64(3.14159))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        ReadAngle(results[0]);
    }

    [TestMethod]
    public void f64AngleLib_FromDegrees()
    {
        using var state = CreateState();
        var results = state.DoString("return f64anglelib.from_degrees(fixed64(90))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual((Fixed64)90, ReadAngle(results[0]).Degrees);
    }

    [TestMethod]
    public void f64AngleLib_Wrap()
    {
        using var state = CreateState();
        var results = state.DoString("return f64anglelib.wrap(f64angle(380))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        ReadAngle(results[0]);
    }

    [TestMethod]
    public void f64AngleLib_WrapPositive()
    {
        using var state = CreateState();
        var results = state.DoString("return f64anglelib.wrap_positive(f64angle(-90))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        ReadAngle(results[0]);
    }

    [TestMethod]
    public void f64AngleLib_Min()
    {
        using var state = CreateState();
        var results = state.DoString("return f64anglelib.min(f64angle(30), f64angle(60))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual((Fixed64)30, ReadAngle(results[0]).Degrees);
    }

    [TestMethod]
    public void f64AngleLib_Max()
    {
        using var state = CreateState();
        var results = state.DoString("return f64anglelib.max(f64angle(30), f64angle(60))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual((Fixed64)60, ReadAngle(results[0]).Degrees);
    }

    [TestMethod]
    public void f64AngleLib_Degrees()
    {
        using var state = CreateState();
        var results = state.DoString("return f64anglelib.degrees(f64angle(45))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual((Fixed64)45, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64AngleLib_Radians()
    {
        using var state = CreateState();
        var results = state.DoString("return f64anglelib.radians(f64angle(180))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
    }

    // ---- f64eulerlib functions ----

    [TestMethod]
    public void f64EulerLib_Wrap()
    {
        using var state = CreateState();
        var results = state.DoString("return f64eulerlib.wrap(f64euler(380, 0, 0))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        ReadEuler(results[0]);
    }

    [TestMethod]
    public void f64EulerLib_WrapPositive()
    {
        using var state = CreateState();
        var results = state.DoString("return f64eulerlib.wrap_positive(f64euler(-90, 0, 0))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        ReadEuler(results[0]);
    }

    // ---- Metatable __index field access ----

    [TestMethod]
    public void Metatable_Fixed64_Raw()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64(42).raw");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Number, results[0].Type);
        Assert.IsTrue(results[0].TryRead<double>(out var d));
        Assert.IsTrue(d > 0);
    }

    [TestMethod]
    public void Metatable_Fixed64Vector3_X()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64vector3(1, 2, 3).x");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        Assert.AreEqual((Fixed64)1, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void Metatable_Fixed64Vector3_Y()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64vector3(1, 2, 3).y");

        Assert.AreEqual((Fixed64)2, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void Metatable_Fixed64Vector3_Z()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64vector3(1, 2, 3).z");

        Assert.AreEqual((Fixed64)3, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void Metatable_Fixed64Vector3_UnknownField_Nil()
    {
        using var state = CreateState();
        var results = state.DoString("return fixed64vector3(1, 2, 3).w");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Nil, results[0].Type);
    }

    [TestMethod]
    public void Metatable_f64Angle_Deg()
    {
        using var state = CreateState();
        var results = state.DoString("return f64angle(90).deg");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        Assert.AreEqual((Fixed64)90, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void Metatable_f64Angle_Rad()
    {
        using var state = CreateState();
        var results = state.DoString("return f64angle(180).rad");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        // 180° = π ≈ 3.14159 in radians
        Assert.IsTrue((double)ReadFixed64(results[0]) is >= 3.14 and <= 3.142);
    }

    [TestMethod]
    public void Metatable_f64Angle_UnknownField_Nil()
    {
        using var state = CreateState();
        var results = state.DoString("return f64angle(45).unknown");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Nil, results[0].Type);
    }

    [TestMethod]
    public void Metatable_f64Euler_Yaw()
    {
        using var state = CreateState();
        var results = state.DoString("return f64euler(10, 20, 30).yaw");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual((Fixed64)10, ReadAngle(results[0]).Degrees);
    }

    [TestMethod]
    public void Metatable_f64Euler_Pitch()
    {
        using var state = CreateState();
        var results = state.DoString("return f64euler(10, 20, 30).pitch");

        Assert.AreEqual((Fixed64)20, ReadAngle(results[0]).Degrees);
    }

    [TestMethod]
    public void Metatable_f64Euler_Roll()
    {
        using var state = CreateState();
        var results = state.DoString("return f64euler(10, 20, 30).roll");

        Assert.AreEqual((Fixed64)30, ReadAngle(results[0]).Degrees);
    }

    [TestMethod]
    public void Metatable_f64Euler_UnknownField_Nil()
    {
        using var state = CreateState();
        var results = state.DoString("return f64euler(0, 0, 0).x");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Nil, results[0].Type);
    }

    [TestMethod]
    public void Metatable_f64Euler_ChainedAccess()
    {
        using var state = CreateState();
        var results = state.DoString("return f64euler(45, 30, 15).yaw.deg");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        Assert.AreEqual((Fixed64)45, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void Metatable_fixed64_Raw_UsedInArithmetic()
    {
        using var state = CreateState();
        var results = state.DoString("local r = fixed64(10).raw; return r + 5");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Number, results[0].Type);
        Assert.IsTrue(results[0].TryRead<double>(out var d));
        Assert.IsTrue(d > 5);
    }

    // ---- f64math constants ----

    [TestMethod]
    public void f64math_Pi()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.pi");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
        Assert.AreEqual(FixedMath.PI, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_HalfPi()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.halfpi");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(FixedMath.PiOver2, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_TwoPi()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.twopi");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(FixedMath.TwoPI, ReadFixed64(results[0]));
    }

    // ---- f64math scalar functions ----

    [TestMethod]
    public void f64math_Sin_Zero()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.sin(fixed64(0))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(Fixed64.Zero, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_Sin_Pi()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.sin(f64math.pi)");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(Fixed64.Zero, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_Cos_Zero()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.cos(fixed64(0))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(Fixed64.One, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_Tan_Zero()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.tan(fixed64(0))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(Fixed64.Zero, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_Floor()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.floor(fixed64(3.7))");

        Assert.AreEqual((Fixed64)3, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_Ceil()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.ceil(fixed64(3.2))");

        Assert.AreEqual((Fixed64)4, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_Round()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.round(fixed64(2.6))");

        Assert.AreEqual((Fixed64)3, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_Abs()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.abs(fixed64(-5))");

        Assert.AreEqual((Fixed64)5, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_Sqrt()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.sqrt(fixed64(9))");

        Assert.AreEqual(1, results.Length);
        Assert.IsTrue((double)ReadFixed64(results[0]) is >= 2.999 and <= 3.001);
    }

    [TestMethod]
    public void f64math_Pow()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.pow(fixed64(2), fixed64(3))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual((Fixed64)8, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_Min()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.min(fixed64(3), fixed64(5))");

        Assert.AreEqual((Fixed64)3, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_Max()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.max(fixed64(3), fixed64(5))");

        Assert.AreEqual((Fixed64)5, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_Clamp()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.clamp(fixed64(7), fixed64(0), fixed64(5))");

        Assert.AreEqual((Fixed64)5, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_Clamp01()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.clamp01(fixed64(1.5))");

        Assert.AreEqual(Fixed64.One, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_Sign()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.sign(fixed64(-3))");

        Assert.AreEqual((Fixed64)(-1), ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_Lerp()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.lerp(fixed64(0), fixed64(10), fixed64(0.5))");

        Assert.AreEqual((Fixed64)5, ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_Deg2Rad()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.deg2rad(fixed64(180))");

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(FixedMath.DegToRad((Fixed64)180), ReadFixed64(results[0]));
    }

    [TestMethod]
    public void f64math_Rad2Deg()
    {
        using var state = CreateState();
        var results = state.DoString("return f64math.rad2deg(f64math.pi)");

        Assert.AreEqual(1, results.Length);
        // 180° from π; fixed-point round-trips within a small epsilon.
        Assert.IsTrue((double)ReadFixed64(results[0]) is >= 179.9 and <= 180.1);
    }
}
