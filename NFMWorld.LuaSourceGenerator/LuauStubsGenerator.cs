using System.Text;
using Microsoft.CodeAnalysis;
using WorldXaml.Generator.Common;

namespace NFMWorld.LuaSourceGenerator;

/// <summary>
/// Generates Lua Language Server (LuaLS) annotation stubs for [LuaVisible] types.
/// Outputs ---@class, ---@field, ---@param, ---@return annotations for IDE autocomplete.
/// </summary>
internal sealed class LuauStubsGenerator(BaseLuaTypeMetadata type)
{
    public string GenerateCode()
    {
        var sb = new IndentedStringBuilder();

        // Instance annotation (for objects created via .new())
        GenerateInstanceClass(sb);

        // Static/class annotation (for TypeTable access)
        GenerateClassAnnotation(sb);

        return sb.ToString();
    }

    private void GenerateInstanceClass(IndentedStringBuilder sb)
    {
        var luaName = type.LuaName;

        // Build base type list for ---@class, filtering self-references and ILuaUserData
        var baseTypes = new List<string>();
        if (type is LuaTypeMetadata { BaseType: { } baseType } && baseType.HasLuaVisibleAttr)
        {
            var baseStub = baseType.LuaName;
            if (baseStub != luaName)
                baseTypes.Add(baseStub);
        }

        if (baseTypes.Count > 0)
            sb.AppendLine($"declare extern type {luaName} extends {baseTypes[0]} with");
        else
            sb.AppendLine($"declare extern type {luaName} with");

        if (type is LuaTypeMetadata luaTypeMetadata)
        {
            using (sb.Indent())
            {
                // Fields and properties
                foreach (var prop in luaTypeMetadata.InstanceProperties.Where(p => p.HasGetter))
                    sb.AppendLine($"{prop.LuaName}: {ToLuaTypeName(prop.PropertyType)}");

                foreach (var field in luaTypeMetadata.InstanceFields)
                    sb.AppendLine($"{field.LuaName}: {ToLuaTypeName(field.FieldType)}");

                // Instance methods
                foreach (var group in luaTypeMetadata.InstanceMethods.GroupBy(m => m.LuaName))
                {
                    sb.Append($"{group.Key}: ");
                    
                    // overloads
                    foreach (var (m, gidx) in group.Select((e, idx) => (e, idx)))
                    {
                        if (gidx != 0)
                        {
                            sb.Append(" & ");
                        }
                        
                        sb.Append("((");
                        foreach (var (p, idx) in m.Parameters.Select((e, idx) => (e, idx)))
                        {
                            if (idx != 0) sb.Append(", ");
                            sb.Append($"{ParamName(p, idx)}: {ToLuaTypeName(p.Type)}");
                        }

                        sb.Append(") -> ");
                        if (!m.IsVoid)
                        {
                            sb.Append(ToLuaTypeName(m.ReturnType));
                        }
                        else
                        {
                            sb.Append("nil");
                        }

                        sb.Append(")");
                    }

                    sb.AppendLine();
                }
            }
        }

        sb.AppendLine("end");

        sb.AppendLine();
    }

    private void GenerateClassAnnotation(IndentedStringBuilder sb)
    {
        var luaName = type.LuaName;

        sb.AppendLine($"declare {luaName}: {{");

        if (type is LuaTypeMetadata luaTypeMetadata)
        {
            using (sb.Indent())
            {
                // Static properties and fields
                foreach (var prop in luaTypeMetadata.StaticProperties.Where(p => p.HasGetter))
                {
                    sb.AppendLine($"{prop.LuaName}: {ToLuaTypeName(prop.PropertyType)},");
                }

                foreach (var field in luaTypeMetadata.StaticFields)
                {
                    sb.AppendLine($"{field.LuaName}: {ToLuaTypeName(field.FieldType)},");
                }

                // Constructors
                GenerateConstructorStubs(sb, luaTypeMetadata);

                // Static methods
                foreach (var group in luaTypeMetadata.StaticMethods.GroupBy(m => m.LuaName))
                {
                    sb.Append($"{group.Key}: ");
                    
                    // overloads
                    foreach (var (m, gidx) in group.Select((e, idx) => (e, idx)))
                    {
                        if (gidx != 0)
                        {
                            sb.Append(" & ");
                        }
                        
                        sb.Append("((");
                        foreach (var (p, idx) in m.Parameters.Select((e, idx) => (e, idx)))
                        {
                            if (idx != 0) sb.Append(", ");
                            sb.Append($"{ParamName(p, idx)}: {ToLuaTypeName(p.Type)}");
                        }

                        sb.Append(") -> ");
                        if (!m.IsVoid)
                        {
                            sb.Append(ToLuaTypeName(m.ReturnType));
                        }
                        else
                        {
                            sb.Append("nil");
                        }

                        sb.Append(")");
                    }

                    sb.AppendLine(",");
                }
            }
        }

        sb.AppendLine("}");
    }

    private static void GenerateConstructorStubs(IndentedStringBuilder sb, LuaTypeMetadata luaTypeMetadata)
    {
        var luaName = luaTypeMetadata.LuaName;

        if (luaTypeMetadata.IsStatic || luaTypeMetadata.IsInterface) return;

        if (luaTypeMetadata.Constructors.Length > 0)
        {
            sb.Append($"new: ");

            // overloads
            foreach (var (m, gidx) in luaTypeMetadata.Constructors.Select((e, idx) => (e, idx)))
            {
                if (gidx != 0)
                {
                    sb.Append(" & ");
                }

                sb.Append("((");
                foreach (var (p, idx) in m.Parameters.Select((e, idx) => (e, idx)))
                {
                    if (idx != 0) sb.Append(", ");
                    sb.Append($"{ParamName(p, idx)}: {ToLuaTypeName(p.Type)}");
                }

                sb.Append(") -> ");
                if (!m.IsVoid)
                {
                    sb.Append(ToLuaTypeName(m.ReturnType));
                }
                else
                {
                    sb.Append("nil");
                }

                sb.Append(")");
            }

            sb.AppendLine(",");
        }
    }

    // ==================================================================
    // Type name conversion helpers
    // ==================================================================

    private static string ToLuaTypeName(BaseLuaTypeMetadata t)
    {
        var suff = t.IsNullableReferenceType || t.IsNullableValueType ? "|nil" : "";

        // A shim override (type-level or member-level) always wins over the
        // default rendering below, so [LuaShimType] can remap e.g. LuaTable to
        // a domain-specific Lua type name.
        if (t.ShimType is { } shimType)
            return ResolveShimType(shimType, t) + suff;

        if (t.SpecialType is SpecialType.System_Boolean) return "boolean";
        if (t.SpecialType is SpecialType.System_Char) return "integer";
        if (t.SpecialType is SpecialType.System_SByte) return "integer";
        if (t.SpecialType is SpecialType.System_Byte) return "integer";
        if (t.SpecialType is SpecialType.System_Int16) return "integer";
        if (t.SpecialType is SpecialType.System_UInt16) return "integer";
        if (t.SpecialType is SpecialType.System_Int32) return "integer";
        if (t.SpecialType is SpecialType.System_UInt32) return "integer";
        if (t.SpecialType is SpecialType.System_Int64) return "integer";
        if (t.SpecialType is SpecialType.System_UInt64) return "integer";
        if (t.SpecialType is SpecialType.System_Decimal) return "number";
        if (t.SpecialType is SpecialType.System_Single) return "number";
        if (t.SpecialType is SpecialType.System_Double) return "number";
        if (t.SpecialType is SpecialType.System_String) return "string";
        if (t.SpecialType is SpecialType.System_Void) return "void";
        if (t.IsFixed64) return "fixed64";
        if (t.IsFixed64AngleSingle) return "f64angle";
        if (t.IsFixed64Euler) return "f64euler";
        if (t.IsFixed64Vector3) return "fixed64vector3";
        
        if (t.IsNullableValueType) return $"{ToLuaTypeName(t.NullableUnderlyingType!)}|nil";

        if (t.IsArray)
        {
            return $"{{ [integer]: {ToLuaTypeName(t.IEnumerableType)}}}{suff}";
        }
        if (t.IsInlineArray)
        {
            return $"{{ [integer]: {ToLuaTypeName(t.InlineArrayElementType)}}}{suff}";
        }

        if (t.FullTypeName == "global::Lua.LuaTable") return $"{{ [any]: any }}{suff}";
        if (t.FullTypeName == "global::Lua.LuaFunction") return $"((...any) -> any){suff}";
        if (t.FullTypeName == "global::Lua.LuaValue") return $"any{suff}";

        return t.LuaName + suff;
    }

    private static string ResolveShimType(string shimType, BaseLuaTypeMetadata t)
    {
        var names = t.ShimTypeTypeParameterNames;
        var args = t.ShimTypeTypeArguments;
        if (names is null || args is null || names.Length != args.Length)
            return shimType;

        var result = shimType;
        // Replace longer parameter names first so "T" doesn't corrupt "TView".
        var pairs = names.Select((n, i) => (Name: n, Arg: args[i])).OrderByDescending(p => p.Name.Length);
        foreach (var (name, arg) in pairs)
            result = result.Replace(name, ToLuaTypeName(arg));

        return result;
    }

    private static string ParamName(LuaParameterMetadata p, int idx) =>
        p.Name ?? $"arg{idx}";
}
