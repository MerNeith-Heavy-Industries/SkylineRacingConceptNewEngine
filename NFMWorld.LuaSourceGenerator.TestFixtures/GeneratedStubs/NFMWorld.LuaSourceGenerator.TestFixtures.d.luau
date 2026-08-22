declare extern type ICalculator with
end

declare ICalculator: {
}

declare extern type CalculatorBase with
end

declare CalculatorBase: {
}

declare extern type DerivedCalculator extends CalculatorBase with
end

declare DerivedCalculator: {
}

declare extern type AnotherCalculator with
end

declare AnotherCalculator: {
}

declare extern type GenericMethods with
end

declare GenericMethods: {
}

declare extern type ExternalTypeUser with
end

declare ExternalTypeUser: {
}

declare extern type IDog with
    breed: string
    name: string
    age: integer
end

declare IDog: {
}

declare extern type IFixtureCar with
    model: string
    isElectric: boolean
    speed: integer
    driverName: string
    x: number
    y: number
    z: number
end

declare IFixtureCar: {
}

declare extern type IPerson with
    email: string
    getName: (() -> string)
    setName: ((name: string) -> nil)
    getAge: (() -> integer)
    setAge: ((age: integer) -> nil)
end

declare IPerson: {
}

declare extern type RecordStructType with
    x: integer
    y: integer
    sum: (() -> integer)
end

declare RecordStructType: {
    new: (() -> nil) & ((x: integer, y: integer) -> nil),
}

declare extern type SampleClass with
    id: integer
    name: string
    isActive: boolean
    value: number
    preciseValue: number
    nullableInt: integer|nil
    nullableFloat: number|nil
    nullableBool: boolean|nil
    publicField: integer
    publicStringField: string
    nullableLongField: integer|nil
    getDoubleId: (() -> integer)
    getGreeting: ((prefix: string) -> string)
    setValue: ((newValue: number) -> nil)
    calculate: ((a: number, b: number, multiply: boolean) -> number)
    clone: (() -> SampleClass)
    setNullableValue: ((newValue: number|nil) -> nil)
    multiplyByNullable: ((multiplier: integer|nil) -> integer|nil)
    formatWithOptional: ((prefix: string, suffix: string) -> string)
    customName: (() -> string)
end

declare SampleClass: {
    staticCounter: integer,
    staticName: string,
    staticNullableDouble: number|nil,
    new: (() -> nil) & ((id: integer, name: string) -> nil) & ((id: integer, name: string, isActive: boolean, value: number) -> nil) & ((nullableId: integer|nil, nullableName: string) -> nil),
    add: ((a: integer, b: integer) -> integer),
    concat: ((a: string, b: string) -> string),
    incrementCounter: (() -> nil),
    addNullable: ((a: integer|nil, b: integer|nil) -> integer),
    getNullableValue: ((hasValue: boolean, value: integer) -> integer|nil),
}

declare extern type Vec2 with
end

declare Vec2: {
}

declare extern type StaticClass with
end

declare StaticClass: {
    staticProperty: string,
    readOnlyProperty: number,
    staticField: integer,
    getMagicNumber: (() -> integer),
    add: ((a: integer, b: integer) -> integer),
    greet: ((name: string) -> string),
    calculate: ((x: number, y: number, operation: string) -> number),
    raiseMessage: ((message: string) -> nil),
}

declare extern type TypeInLuaNamespace with
    name: string
    value: integer
    getDescription: (() -> string)
end

declare TypeInLuaNamespace: {
    new: (() -> nil) & ((name: string, value: integer) -> nil),
}

declare extern type TypeWithArrays with
end

declare TypeWithArrays: {
}

declare extern type TypeWithByRefParameters with
end

declare TypeWithByRefParameters: {
}

declare extern type TypeWithConstants with
end

declare TypeWithConstants: {
    factor: integer,
    defaultName: string,
    pi: number,
    multiplier: integer,
    applyFactor: ((value: integer) -> integer),
}

declare extern type TestColor with
end

declare TestColor: {
}

declare extern type TypeWithEnum with
    color: TestColor
    readOnlyColor: TestColor
    nullableColor: TestColor|nil
    defaultColor: TestColor
    getColor: (() -> TestColor)
    setColor: ((color: TestColor) -> nil)
    isPrimary: ((color: TestColor) -> boolean)
    getNullableColor: ((returnValue: boolean) -> TestColor|nil)
end

declare TypeWithEnum: {
}

declare extern type TypeWithEvents with
end

declare TypeWithEvents: {
}

declare extern type CustomEventArgs with
end

declare CustomEventArgs: {
}

declare extern type TypeWithExceptions with
end

declare TypeWithExceptions: {
}

declare extern type TypeWithExtensionMembers with
end

declare TypeWithExtensionMembers: {
}

declare extern type TypeWithFixedMathNullables with
    nullableFixed: fixed64|nil
    nullableVec3: fixed64vector3|nil
    normalFixed: fixed64
    normalVec3: fixed64vector3
    getOptionalValue: ((returnValue: boolean) -> fixed64|nil)
end

declare TypeWithFixedMathNullables: {
    new: (() -> nil),
}

declare extern type InlineBuffer with
end

declare InlineBuffer: {
}

declare extern type TypeWithInlineArray with
end

declare TypeWithInlineArray: {
}

declare extern type TypeWithIntIndexer with
end

declare TypeWithIntIndexer: {
}

declare extern type TypeWithMemberShimOverrides with
    myProperty: CustomProperty
    myField: CustomField
    methodWithParamShim: ((value: CustomParam) -> integer)
    methodWithReturnShim: (() -> CustomReturn)
end

declare TypeWithMemberShimOverrides: {
}

declare extern type TypeWithMethodDeduplication with
end

declare TypeWithMethodDeduplication: {
}

declare extern type AnotherCalculator2 with
end

declare AnotherCalculator2: {
}

declare extern type TypeWithNewMember with
end

declare TypeWithNewMember: {
}

declare extern type TypeWithMultiDimArray with
end

declare TypeWithMultiDimArray: {
}

declare extern type TypeWithMultiParamIndexer with
end

declare TypeWithMultiParamIndexer: {
}

declare extern type TypeWithNestedGeneric with
end

declare TypeWithNestedGeneric: {
}

declare extern type TypeWithOverloads with
    value: integer
    text: string
    processNumber: ((x: integer) -> string) & ((x: number) -> string) & ((x: integer) -> string) & ((x: number) -> string)
    processData: ((s: string) -> string) & ((arr: { [integer]: integer}) -> string) & ((arr: { [integer]: number}) -> string) & ((flag: boolean) -> string)
    combine: ((a: integer, b: integer) -> string) & ((a: number, b: number) -> string) & ((a: string, b: string) -> string) & ((a: integer, b: string) -> string) & ((a: string, b: integer) -> string)
end

declare TypeWithOverloads: {
    new: ((value: integer) -> nil) & ((value: number) -> nil) & ((text: string) -> nil),
    staticProcess: ((x: integer) -> string) & ((x: number) -> string) & ((s: string) -> string),
}

declare extern type TypeWithReferences with
end

declare TypeWithReferences: {
}

declare extern type TypeWithSpanParameters with
    name: string
    getName: (() -> string)
end

declare TypeWithSpanParameters: {
    new: (() -> nil),
}

declare extern type TypeWithStaticAbstractInterface with
end

declare TypeWithStaticAbstractInterface: {
}

declare extern type TypeWithStringIndexer with
end

declare TypeWithStringIndexer: {
}

declare extern type TypeWithTupleOverloads with
    processTuple: ((coords: System.ValueTuple_int_int) -> string) & ((point: System.ValueTuple_int_int_int) -> string)
    processMixed: ((id: integer, data: System.ValueTuple_string_bool) -> string)
    combine: ((a: System.ValueTuple_int_int, b: System.ValueTuple_int_int) -> string) & ((a: System.ValueTuple_int_int_int, b: System.ValueTuple_int_int_int) -> string) & ((a: System.ValueTuple_int_int, scalar: integer) -> string)
end

declare TypeWithTupleOverloads: {
    new: (() -> nil),
}

declare extern type Vec3 with
end

declare Vec3: {
}

declare extern type System.Collections.Generic.List_int_Enumerator with
end

declare System.Collections.Generic.List_int_Enumerator: {
}

declare extern type System.Collections.Generic.List_string_Enumerator with
end

declare System.Collections.Generic.List_string_Enumerator: {
}

