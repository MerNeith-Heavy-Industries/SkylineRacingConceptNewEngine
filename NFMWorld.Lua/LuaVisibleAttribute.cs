namespace NFMWorld.Lua;

/// <summary>
/// Marks a type to be exposed to Lua via the source generator.
/// The type will be available as a global variable in Lua with the same name.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum, Inherited = false)]
public sealed class LuaVisibleAttribute : Attribute
{
    /// <summary>
    /// Optional custom name to use in Lua. If not specified, the type name is used.
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// Marks a method or property with a custom Lua name.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Constructor)]
public sealed class LuaNameAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;
}

/// <summary>
/// Overrides how a type is rendered in generated LuaLS shims (data/lua/library/*.lua).
/// Can be applied to a type, or to a parameter, field, property, or return value to
/// override just that member's shim type.
/// For generic types, occurrences of each type parameter name (e.g. "T" in "T[]") are
/// replaced with the shim type name of the corresponding type argument.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface |
    AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue,
    AllowMultiple = false, Inherited = false)]
public sealed class LuaShimTypeAttribute(string shimType) : Attribute
{
    public string ShimType { get; } = shimType;
}

/// <summary>
/// Defines the overload priority of a method overload when binding to Lua. The default priority is 1.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LuaOverloadPriorityAttribute(long priority) : Attribute
{
    public long OverloadPriority { get; } = priority;
}
