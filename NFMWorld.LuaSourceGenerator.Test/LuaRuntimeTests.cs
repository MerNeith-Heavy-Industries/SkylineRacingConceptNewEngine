using NuLua;
using NuLua.Luau;
using NFMWorld.LuaSourceGenerator.Generator.NFMWorld.LuaSourceGenerator.TestFixtures;
using NFMWorld.LuaSourceGenerator.TestFixtures;

namespace NFMWorld.LuaSourceGenerator.Test;

/// <summary>
/// FixedMath primitive tests. fixed64 / f64angle / f64euler / fixed64vector3 marshal to Lua via
/// <see cref="LuaRefValue.FromPrimitive"/> (ids 0..3); their per-state metatables and read-back
/// (<c>TryRead&lt;Fixed64&gt;</c> etc.) are not wired up yet — round-trips are a later pass.
/// </summary>
[TestClass]
public class LuaRuntimeTests
{
    private LuauState _state = null!;

    [TestInitialize]
    public void Setup()
    {
        _state = LuauState.Create();
        _state.OpenLibraries();
        LuaVisibleTypeRegistry.RegisterAll(_state);
    }

    [TestCleanup]
    public void TearDown()
    {
        _state.Dispose();
    }

    [TestMethod]
    public void Fixed64_ReadMarshalsAsPrimitive()
    {
        // Reading a Fixed64 property marshals via LuaValue.FromPrimitive(id 0).
        var results = _state.DoString(@"
            local obj = TypeWithFixedMathNullables.new()
            return obj.normalFixed
        ");
        Assert.AreEqual(LuaValueType.Primitive, results[0].Type);
    }

    [TestMethod]
    public void Fixed64_NullableNull_ReadsNil()
    {
        var results = _state.DoString(@"
            local obj = TypeWithFixedMathNullables.new()
            return obj.nullableFixed
        ");
        Assert.AreEqual(LuaValueType.Nil, results[0].Type);
    }

    [TestMethod]
    public void Fixed64_Write_FromNumber_IsDeferred()
    {
        // Writing a Fixed64 from a Lua number currently raises (primitive read-back not implemented).
        var results = _state.DoString(@"
            local obj = TypeWithFixedMathNullables.new()
            local ok = pcall(function() obj.normalFixed = 1.0 end)
            return ok
        ");
        Assert.IsFalse(results[0].Read<bool>());
    }
}
