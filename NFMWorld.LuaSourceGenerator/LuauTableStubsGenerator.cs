using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using WorldXaml.Generator.Common;

namespace NFMWorld.LuaSourceGenerator;

internal sealed class LuauTableStubsGenerator(LuaTypeMetadata type)
{
    public string GenerateCode()
    {
        var sb = new IndentedStringBuilder();

        // Instance annotation (for objects created via .new())
        GenerateInstanceClass(sb);

        return sb.ToString();
    }

    private void GenerateInstanceClass(IndentedStringBuilder sb)
    {
        var luaName = SanitizeLongTypeName(type.TypeName);
        
        sb.AppendLine($"export type {luaName} = {{");

        using (sb.Indent())
        {
            // Fields and properties
            foreach (var prop in type.InstanceProperties.Where(p => p.HasGetter))
                sb.AppendLine($"{prop.LuaName}: {ToLuaTypeName(prop.PropertyType)},");

            foreach (var field in type.InstanceFields)
                sb.AppendLine($"{field.LuaName}: {ToLuaTypeName(field.FieldType)},");
        }

        sb.AppendLine("}");

        sb.AppendLine();
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

        if (t.IsArray || t.IsIList)
        {
            return $"{{{ToLuaTypeName(t.IEnumerableType!)}}}{suff}";
        }
        if (t.IsInlineArray)
        {
            return $"{{{ToLuaTypeName(t.InlineArrayElementType)}}}{suff}";
        }

        if (t.FullTypeName == "global::Lua.LuaTable") return $"{{ [any]: any }}{suff}";
        if (t.FullTypeName == "global::Lua.LuaFunction") return $"((...any) -> any){suff}";
        if (t.FullTypeName == "global::Lua.LuaValue") return $"any{suff}";

        return SanitizeLongTypeName(t.TypeName) + suff;
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
    
    private static string SanitizeLongTypeName(string fullTypeName)
    {
        // Map primitives to short names
        if (fullTypeName is
            "int" or "long" or "float" or "double" or
            "bool" or "string" or "byte" or "sbyte" or
            "short" or "ushort" or "uint" or "ulong" or
            "decimal" or "char" or "object")
            return fullTypeName;

        return Regex.Replace(fullTypeName, @"\[,*\]", match => "Array" + (match.Value.Count(c => c == ',') is var v and >= 1 ? $"{v+1}" : ""))
            .Replace("<", "_")
            .Replace(">", "")
            .Replace(".", "_")
            .Replace("?", "n")
            .Replace("(", "ValueTuple_")
            .Replace(", ", "_")
            .Replace(")", "_")
            .Replace("[", "")
            .Replace("]", "")
            .Replace(",", "_")
            .Replace(" ", "_")
            .Replace("*", "Ptr")
            .Replace("global::", "")
            .Replace("@", "_")
            .TrimEnd('_');
    }

}
