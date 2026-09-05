using NuLua;
using NuLua.Luau;
using NFMWorld.LuaSourceGenerator.Generator.NFMWorld.LuaSourceGenerator.TestFixtures;
using NFMWorld.LuaSourceGenerator.Test.SampleTypes;
using NFMWorld.LuaSourceGenerator.TestFixtures;

namespace NFMWorld.LuaSourceGenerator.Test;

/// <summary>
/// Runtime tests for the NuLua.Luau-targeted generator output. Exercises generated bindings
/// (ILuaUserData&lt;T&gt; partial types, per-state <c>LuaVisibleTypeRegistry.RegisterAll</c>,
/// enum/userdata marshalling, operators) from the Lua side.
/// </summary>
[TestClass]
public class LuaRuntimePortedTests
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

    // ===================================================================
    // SampleClass tests
    // ===================================================================

    [TestMethod]
    public void SampleClass_Constructor_Default()
    {
        var results = _state.DoString(@"
            local obj = SampleClass.new()
            return obj.id, obj.name
        ");
        Assert.AreEqual(0, results[0].Read<int>());
        Assert.AreEqual("", results[1].Read<string>());
    }

    [TestMethod]
    public void SampleClass_Constructor_WithIdAndName()
    {
        var results = _state.DoString(@"
            local obj = SampleClass.new(42, 'TestName')
            return obj.id, obj.name
        ");
        Assert.AreEqual(42, results[0].Read<int>());
        Assert.AreEqual("TestName", results[1].Read<string>());
    }

    [TestMethod]
    public void SampleClass_Constructor_Full()
    {
        var results = _state.DoString(@"
            local obj = SampleClass.new(10, 'FullTest', true, 3.14)
            return obj.id, obj.name, obj.isActive, obj.value
        ");
        Assert.AreEqual(10, results[0].Read<int>());
        Assert.AreEqual("FullTest", results[1].Read<string>());
        Assert.IsTrue(results[2].Read<bool>());
        Assert.AreEqual(3.14, results[3].Read<double>(), 0.01);
    }

    [TestMethod]
    public void SampleClass_PropertySet_ModifiesObject()
    {
        var results = _state.DoString(@"
            local obj = SampleClass.new()
            obj.id = 100
            obj.name = 'Modified'
            obj.isActive = true
            obj.value = 9.99
            return obj.id, obj.name, obj.isActive, obj.value
        ");
        Assert.AreEqual(100, results[0].Read<int>());
        Assert.AreEqual("Modified", results[1].Read<string>());
        Assert.IsTrue(results[2].Read<bool>());
        Assert.AreEqual(9.99, results[3].Read<double>(), 0.01);
    }

    [TestMethod]
    public void SampleClass_InstanceMethod_GetDoubleId()
    {
        var results = _state.DoString(@"
            local obj = SampleClass.new(21, 'Test')
            return obj:getDoubleId()
        ");
        Assert.AreEqual(42, results[0].Read<int>());
    }

    [TestMethod]
    public void SampleClass_InstanceMethod_GetGreeting()
    {
        var results = _state.DoString(@"
            local obj = SampleClass.new(1, 'World')
            return obj:getGreeting('Hello')
        ");
        Assert.AreEqual("Hello World!", results[0].Read<string>());
    }

    [TestMethod]
    public void SampleClass_InstanceMethod_SetValue()
    {
        var results = _state.DoString(@"
            local obj = SampleClass.new()
            obj:setValue(42.5)
            return obj.value
        ");
        Assert.AreEqual(42.5, results[0].Read<double>(), 0.01);
    }

    [TestMethod]
    public void SampleClass_InstanceMethod_Calculate()
    {
        var results = _state.DoString(@"
            local obj = SampleClass.new()
            local add = obj:calculate(3, 4, false)
            local mul = obj:calculate(3, 4, true)
            return add, mul
        ");
        Assert.AreEqual(7, results[0].Read<double>(), 0.01);
        Assert.AreEqual(12, results[1].Read<double>(), 0.01);
    }

    [TestMethod]
    public void SampleClass_InstanceMethod_Clone()
    {
        var results = _state.DoString(@"
            local obj = SampleClass.new(42, 'Original')
            local clone = obj:clone()
            clone.name = 'Cloned'
            return obj.name, clone.name, clone.id
        ");
        Assert.AreEqual("Original", results[0].Read<string>());
        Assert.AreEqual("Cloned", results[1].Read<string>());
        Assert.AreEqual(42, results[2].Read<int>());
    }

    [TestMethod]
    public void SampleClass_InstanceMethod_CustomName()
    {
        var results = _state.DoString(@"
            local obj = SampleClass.new()
            return obj:customName()
        ");
        Assert.AreEqual("custom", results[0].Read<string>());
    }

    [TestMethod]
    public void SampleClass_StaticMethod_Add()
    {
        var results = _state.DoString("return SampleClass.add(10, 20)");
        Assert.AreEqual(30, results[0].Read<int>());
    }

    [TestMethod]
    public void SampleClass_StaticMethod_Concat()
    {
        var results = _state.DoString("return SampleClass.concat('Hello', ' World')");
        Assert.AreEqual("Hello World", results[0].Read<string>());
    }

    [TestMethod]
    public void SampleClass_StaticProperty_Counter()
    {
        SampleClass.StaticCounter = 0;

        var results = _state.DoString(@"
            local before = SampleClass.staticCounter
            SampleClass.incrementCounter()
            SampleClass.incrementCounter()
            local after = SampleClass.staticCounter
            return before, after
        ");
        Assert.AreEqual(0, results[0].Read<int>());
        Assert.AreEqual(2, results[1].Read<int>());
    }

    [TestMethod]
    public void SampleClass_StaticProperty_Name()
    {
        var results = _state.DoString("return SampleClass.staticName");
        Assert.AreEqual("SampleClass", results[0].Read<string>());
    }

    [TestMethod]
    public void SampleClass_StaticProperty_Counter_ReadWrite()
    {
        SampleClass.StaticCounter = 0;

        _state.DoString("SampleClass.staticCounter = 100");
        Assert.AreEqual(100, SampleClass.StaticCounter);

        var results = _state.DoString("return SampleClass.staticCounter");
        Assert.AreEqual(100, results[0].Read<int>());
    }

    [TestMethod]
    public void SampleClass_Tostring()
    {
        var results = _state.DoString(@"
            local obj = SampleClass.new(42, 'Test', true, 3.14)
            return tostring(obj)
        ");
        var str = results[0].Read<string>();
        Assert.IsTrue(str.Contains("42"));
        Assert.IsTrue(str.Contains("Test"));
    }

    [TestMethod]
    public void SampleClass_InstanceProperty_PreciseValue()
    {
        var results = _state.DoString(@"
            local obj = SampleClass.new()
            obj.preciseValue = 3.141592653589793
            return obj.preciseValue
        ");
        Assert.AreEqual(3.141592653589793, results[0].Read<double>(), 0.0001);
    }

    [TestMethod]
    public void SampleClass_PublicField_ReadWrite()
    {
        var results = _state.DoString(@"
            local obj = SampleClass.new()
            obj.publicField = 12345
            return obj.publicField
        ");
        Assert.AreEqual(12345, results[0].Read<int>());
    }

    [TestMethod]
    public void SampleClass_PublicStringField()
    {
        var results = _state.DoString(@"
            local obj = SampleClass.new()
            obj.publicStringField = 'hello field'
            return obj.publicStringField
        ");
        Assert.AreEqual("hello field", results[0].Read<string>());
    }

    [TestMethod]
    public void SampleClass_BooleanFalse_RoundTrip()
    {
        var results = _state.DoString(@"
            local obj = SampleClass.new(0, '', false, 0)
            return obj.isActive
        ");
        Assert.IsFalse(results[0].Read<bool>());
    }

    [TestMethod]
    public void SampleClass_ReadOnlyProperty()
    {
        var results = _state.DoString(@"
            local obj = SampleClass.new(99, 'ReadOnly')
            return obj.id
        ");
        Assert.AreEqual(99, results[0].Read<int>());
    }

    // ===================================================================
    // Nullable tests
    // ===================================================================

    [TestMethod]
    public void Nullable_Int_ReadNull()
    {
        var obj = new SampleClass();
        _state["obj"] = LuaRefValue.FromUserData(_state.CreateUserData(obj));
        var results = _state.DoString("return obj.nullableInt");
        Assert.AreEqual(LuaValueType.Nil, results[0].Type);
    }

    [TestMethod]
    public void Nullable_Int_SetAndRead()
    {
        var obj = new SampleClass();
        _state["obj"] = LuaRefValue.FromUserData(_state.CreateUserData(obj));
        _state.DoString("obj.nullableInt = 42");
        Assert.AreEqual(42, obj.NullableInt);
    }

    [TestMethod]
    public void Nullable_Int_SetToNil()
    {
        var obj = new SampleClass { NullableInt = 42 };
        _state["obj"] = LuaRefValue.FromUserData(_state.CreateUserData(obj));
        _state.DoString("obj.nullableInt = nil");
        Assert.IsNull(obj.NullableInt);
    }

    [TestMethod]
    public void Nullable_Bool_ThreeState()
    {
        var obj = new SampleClass();
        _state["obj"] = LuaRefValue.FromUserData(_state.CreateUserData(obj));

        var r1 = _state.DoString("return obj.nullableBool");
        Assert.AreEqual(LuaValueType.Nil, r1[0].Type);

        _state.DoString("obj.nullableBool = true");
        Assert.IsTrue(obj.NullableBool);

        _state.DoString("obj.nullableBool = false");
        Assert.IsFalse(obj.NullableBool);

        _state.DoString("obj.nullableBool = nil");
        Assert.IsNull(obj.NullableBool);
    }

    [TestMethod]
    public void Nullable_Float_RoundTrip()
    {
        var obj = new SampleClass();
        _state["obj"] = LuaRefValue.FromUserData(_state.CreateUserData(obj));
        _state.DoString("obj.nullableFloat = 3.14");
        Assert.AreEqual(3.14f, obj.NullableFloat!.Value, 0.01f);
    }

    [TestMethod]
    public void Nullable_Long_Field()
    {
        var obj = new SampleClass { NullableLongField = 1234567890123L };
        _state["obj"] = LuaRefValue.FromUserData(_state.CreateUserData(obj));
        var results = _state.DoString("return obj.nullableLongField");
        Assert.AreEqual(1234567890123.0, results[0].Read<double>(), 1.0);
    }

    // ===================================================================
    // Record struct tests
    // ===================================================================

    [TestMethod]
    public void RecordStruct_CreateAndRead()
    {
        var results = _state.DoString(@"
            local obj = RecordStructType.new(10, 20)
            return obj.x, obj.y
        ");
        Assert.AreEqual(10, results[0].Read<int>());
        Assert.AreEqual(20, results[1].Read<int>());
    }

    [TestMethod]
    public void RecordStruct_DefaultConstructor()
    {
        var results = _state.DoString(@"
            local obj = RecordStructType.new()
            return obj.x, obj.y
        ");
        Assert.AreEqual(0, results[0].Read<int>());
        Assert.AreEqual(0, results[1].Read<int>());
    }

    [TestMethod]
    public void RecordStruct_InstanceMethod()
    {
        var results = _state.DoString(@"
            local obj = RecordStructType.new(3, 4)
            return obj:sum()
        ");
        Assert.AreEqual(7, results[0].Read<int>());
    }

    // ===================================================================
    // RegisterAll / compile checks
    // ===================================================================

    [TestMethod]
    public void RegisterAll_ExposesTypesAsGlobals()
    {
        // TypeWithTupleOverloads, TypeInLuaNamespace and TypeWithFixedMathNullables
        // must be registered as globals by RegisterAll.
        var results = _state.DoString("return TypeWithTupleOverloads ~= nil, TypeInLuaNamespace ~= nil, TypeWithFixedMathNullables ~= nil, Vec2 ~= nil, Vec3 ~= nil");
        Assert.IsTrue(results[0].Read<bool>());
        Assert.IsTrue(results[1].Read<bool>());
        Assert.IsTrue(results[2].Read<bool>());
        Assert.IsTrue(results[3].Read<bool>());
        Assert.IsTrue(results[4].Read<bool>());
    }

    [TestMethod]
    public void LuaNamespace_CreateAndRead()
    {
        var results = _state.DoString(@"
            local obj = TypeInLuaNamespace.new('Test', 99)
            return obj.name, obj.value
        ");
        Assert.AreEqual("Test", results[0].Read<string>());
        Assert.AreEqual(99, results[1].Read<int>());
    }

    [TestMethod]
    public void SpanParams_SpanMethodsNotExposed()
    {
        var results = _state.DoString(@"
            local obj = TypeWithSpanParameters.new()
            return obj:getName(), obj.sum, obj.fill, obj.getChars, obj.countMatching
        ");
        Assert.AreEqual("", results[0].Read<string>());
        Assert.AreEqual(LuaValueType.Nil, results[1].Type); // sum: skipped (ReadOnlySpan param)
        Assert.AreEqual(LuaValueType.Nil, results[2].Type); // fill: skipped (Span param)
        Assert.AreEqual(LuaValueType.Nil, results[3].Type); // getChars: skipped (returns ref struct)
        Assert.AreEqual(LuaValueType.Nil, results[4].Type); // countMatching: skipped (ReadOnlySpan param)
    }

    // ===================================================================
    // Const field tests
    // ===================================================================

    [TestMethod]
    public void ConstFields_Readable()
    {
        var results = _state.DoString(@"
            return TypeWithConstants.factor, TypeWithConstants.defaultName, TypeWithConstants.pi
        ");
        Assert.AreEqual(100, results[0].Read<int>());
        Assert.AreEqual("Default", results[1].Read<string>());
        Assert.AreEqual(3.14159, results[2].Read<double>(), 0.001);
    }

    [TestMethod]
    public void ConstFields_WritableFieldStillWorks()
    {
        _state.DoString("TypeWithConstants.multiplier = 5");
        Assert.AreEqual(5, TypeWithConstants.Multiplier);
    }

    // ===================================================================
    // Interface inheritance — DEFERRED
    // -------------------------------------------------------------------
    // Marshalling a non-[LuaVisible] concrete implementation via a
    // [LuaVisible] interface (e.g. CreateUserData<IDog>(dog)) is deferred:
    // calls through the ILuaUserData<T> constraint inside CreateUserData<T>
    // resolve to the base interface's default members, not the derived
    // interface's default interface methods, so __index/SupportedMetamethods
    // fall back to the base defaults. Revisit in a later pass.
    // ===================================================================

    // ===================================================================
    // Enum marshalling tests
    // ===================================================================

    private void SetEnum(string name, TestColor value)
    {
        _state[name] = LuaRefValue.FromUserData(_state.CreateEnumUserData(value));
    }

    [TestMethod]
    public void Enum_Property_Read()
    {
        var obj = new TypeWithEnum { Color = TestColor.Green };
        _state["obj"] = LuaRefValue.FromUserData(_state.CreateUserData(obj));

        var results = _state.DoString(@"
            local c = obj.color
            return tostring(c)
        ");
        Assert.AreEqual("Green", results[0].Read<string>());
    }

    [TestMethod]
    public void Enum_Property_Set()
    {
        var obj = new TypeWithEnum();
        _state["obj"] = LuaRefValue.FromUserData(_state.CreateUserData(obj));

        SetEnum("blueColor", TestColor.Blue);
        _state.DoString("obj.color = blueColor");
        Assert.AreEqual(TestColor.Blue, obj.Color);
    }

    [TestMethod]
    public void Enum_Method_Return()
    {
        var obj = new TypeWithEnum { Color = TestColor.Blue };
        _state["obj"] = LuaRefValue.FromUserData(_state.CreateUserData(obj));

        var results = _state.DoString(@"
            local c = obj:getColor()
            return tostring(c)
        ");
        Assert.AreEqual("Blue", results[0].Read<string>());
    }

    [TestMethod]
    public void Enum_Method_Parameter()
    {
        var obj = new TypeWithEnum();
        _state["obj"] = LuaRefValue.FromUserData(_state.CreateUserData(obj));

        SetEnum("greenColor", TestColor.Green);
        _state.DoString("obj:setColor(greenColor)");
        Assert.AreEqual(TestColor.Green, obj.Color);
    }

    [TestMethod]
    public void Enum_Method_BoolReturn()
    {
        var obj = new TypeWithEnum();
        _state["obj"] = LuaRefValue.FromUserData(_state.CreateUserData(obj));

        SetEnum("redColor", TestColor.Red);
        SetEnum("yellowColor", TestColor.Yellow);

        var results = _state.DoString(@"
            local r1 = obj:isPrimary(redColor)
            local r2 = obj:isPrimary(yellowColor)
            return r1, r2
        ");
        Assert.IsTrue(results[0].Read<bool>());
        Assert.IsFalse(results[1].Read<bool>());
    }

    [TestMethod]
    public void Enum_Nullable_ReadNull()
    {
        var obj = new TypeWithEnum();
        _state["obj"] = LuaRefValue.FromUserData(_state.CreateUserData(obj));

        var results = _state.DoString("return obj.nullableColor");
        Assert.AreEqual(LuaValueType.Nil, results[0].Type);
    }

    [TestMethod]
    public void Enum_Nullable_SetAndRead()
    {
        var obj = new TypeWithEnum();
        _state["obj"] = LuaRefValue.FromUserData(_state.CreateUserData(obj));

        SetEnum("greenColor", TestColor.Green);
        _state.DoString("obj.nullableColor = greenColor");
        Assert.AreEqual(TestColor.Green, obj.NullableColor);

        var results = _state.DoString("return tostring(obj.nullableColor)");
        Assert.AreEqual("Green", results[0].Read<string>());
    }

    [TestMethod]
    public void Enum_Nullable_SetToNil()
    {
        var obj = new TypeWithEnum { NullableColor = TestColor.Red };
        _state["obj"] = LuaRefValue.FromUserData(_state.CreateUserData(obj));

        _state.DoString("obj.nullableColor = nil");
        Assert.IsNull(obj.NullableColor);
    }

    // ===================================================================
    // Overload resolution tests
    // ===================================================================

    [TestMethod]
    public void ConstructorOverloads_AllVariants()
    {
        var r1 = _state.DoString(@"
            local obj = TypeWithOverloads.new(42)
            return obj.value
        ");
        Assert.AreEqual(42, r1[0].Read<int>());

        var r2 = _state.DoString(@"
            local obj = TypeWithOverloads.new(3.14)
            return obj.value
        ");
        Assert.AreEqual(3, r2[0].Read<int>());

        var r3 = _state.DoString(@"
            local obj = TypeWithOverloads.new('Hello')
            return obj.text
        ");
        Assert.AreEqual("string:Hello", r3[0].Read<string>());
    }

    [TestMethod]
    public void OverloadResolution_ProcessNumber_DispatchesByNumberType()
    {
        var results = _state.DoString(@"
            local obj = TypeWithOverloads.new(0)
            return obj:processNumber(3), obj:processNumber(3.5), obj:processNumber(2.0)
        ");
        Assert.AreEqual("int:3", results[0].Read<string>());
        Assert.AreEqual("double:3.5", results[1].Read<string>());
        // integer-valued doubles match the int overload first
        Assert.AreEqual("int:2", results[2].Read<string>());
    }

    [TestMethod]
    public void OverloadResolution_ProcessData_DispatchesByType()
    {
        var results = _state.DoString(@"
            local obj = TypeWithOverloads.new(0)
            return obj:processData('abc'), obj:processData(true)
        ");
        Assert.AreEqual("string:abc", results[0].Read<string>());
        Assert.AreEqual("bool:True", results[1].Read<string>());
    }

    [TestMethod]
    public void OverloadResolution_Combine_DispatchesByArgumentTypes()
    {
        var results = _state.DoString(@"
            local obj = TypeWithOverloads.new(0)
            return obj:combine(1, 'a'), obj:combine('a', 1), obj:combine(1.5, 2.5), obj:combine('a', 'b')
        ");
        Assert.AreEqual("int,string:1,a", results[0].Read<string>());
        Assert.AreEqual("string,int:a,1", results[1].Read<string>());
        Assert.AreEqual("float,float:1.5,2.5", results[2].Read<string>());
        Assert.AreEqual("string,string:a,b", results[3].Read<string>());
    }

    [TestMethod]
    public void OverloadResolution_StaticProcess_DispatchesByType()
    {
        var results = _state.DoString(@"
            return TypeWithOverloads.staticProcess(1), TypeWithOverloads.staticProcess(2.5), TypeWithOverloads.staticProcess('s')
        ");
        Assert.AreEqual("static:int:1", results[0].Read<string>());
        Assert.AreEqual("static:double:2.5", results[1].Read<string>());
        Assert.AreEqual("static:string:s", results[2].Read<string>());
    }

    [TestMethod]
    public void OverloadResolution_NoMatchingOverload_RaisesLuaError()
    {
        var results = _state.DoString(@"
            local obj = TypeWithOverloads.new(0)
            local ok1 = pcall(function() return obj:combine(1) end)
            local ok2 = pcall(function() return obj:processNumber('x') end)
            return ok1, ok2
        ");
        Assert.IsFalse(results[0].Read<bool>());
        Assert.IsFalse(results[1].Read<bool>());
    }

    // ===================================================================
    // Operator metamethods (same-type operands via ILuaUserData<T>)
    // ===================================================================

    [TestMethod]
    public void Operators_TypeWithOverloads_SameTypeOperands()
    {
        var results = _state.DoString(@"
            local obj = TypeWithOverloads.new(5)
            local obj2 = TypeWithOverloads.new(7)
            local r2 = obj + obj2
            local r4 = obj2 - obj
            return r2.text, r4.text
        ");
        Assert.AreEqual("obj+obj", results[0].Read<string>());
        Assert.AreEqual("obj-obj", results[1].Read<string>());
    }

    [TestMethod]
    public void Operators_Vec3_AddSubNegate()
    {
        _state["v1"] = LuaRefValue.FromUserData(_state.CreateUserData(new Vector3Struct(1, 2, 3)));
        _state["v2"] = LuaRefValue.FromUserData(_state.CreateUserData(new Vector3Struct(10, 20, 30)));

        var results = _state.DoString(@"
            local s = v1 + v2
            local d = v2 - v1
            local n = -v1
            return tostring(s), tostring(d), tostring(n)
        ");
        Assert.AreEqual("Vec3(11, 22, 33)", results[0].Read<string>());
        Assert.AreEqual("Vec3(9, 18, 27)", results[1].Read<string>());
        Assert.AreEqual("Vec3(-1, -2, -3)", results[2].Read<string>());
    }
}
