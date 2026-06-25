using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WorldXaml.Generator.Common;

namespace NFMWorld.Reactor.Generator;

/// <summary>
/// Incremental source generator that finds all non-abstract <see cref="Component"/> subclasses
/// and emits typed <see cref="ComponentNode"/> wrappers with With* methods and factory functions.
/// Constructor parameters become optional factory parameters.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class ReactorComponentFactoryGenerator : IIncrementalGenerator
{
    private const string ComponentFqn = "global::NFMWorld.Reactor.Component";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(spc =>
            spc.AddSource("_ComponentGenPing.g.cs", "// ReactorComponentFactoryGenerator loaded\n"));

        var componentTypes = context.CompilationProvider
            .SelectMany((compilation, _) =>
            {
                var types = new List<ComponentTypeInfo?>();
                SearchSubtypes("NFMWorld.Reactor.Component");

                var seen = new HashSet<string>();
                var deduped = new List<ComponentTypeInfo?>();
                foreach (var t in types)
                {
                    if (t is { } info && seen.Add(info.FullName))
                        deduped.Add(info);
                }
                return System.Collections.Immutable.ImmutableArray.CreateRange(deduped);

                void SearchSubtypes(string baseTypeFqn)
                {
                    var baseType = compilation.GetTypeByMetadataName(baseTypeFqn);
                    if (baseType is null) return;

                    // Only search the current project's own types.
                    CollectSubtypes(compilation.Assembly.GlobalNamespace, baseType, types);
                }
            })
            .WithTrackingName("ComponentTypes");

        context.RegisterSourceOutput(
            componentTypes.Collect(),
            GenerateComponentClasses);
    }

    private static void CollectSubtypes(INamespaceSymbol ns, INamedTypeSymbol baseType, List<ComponentTypeInfo?> results)
    {
        foreach (var member in ns.GetMembers())
        {
            if (member is INamedTypeSymbol type)
            {
                if (ExtendsType(type, baseType))
                {
                    var info = CollectComponentTypeInfo(type);
                    if (info is not null) results.Add(info);
                }
            }
            if (member is INamespaceSymbol child)
                CollectSubtypes(child, baseType, results);
        }
    }

    private static bool ExtendsType(INamedTypeSymbol symbol, INamedTypeSymbol target)
    {
        var targetFqn = target.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var current = symbol.BaseType;
        while (current is not null)
        {
            if (current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == targetFqn)
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static ComponentTypeInfo? CollectComponentTypeInfo(INamedTypeSymbol symbol)
    {
        // Skip abstract types and the Component base class itself
        if (symbol.IsAbstract) return null;
        if (symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == ComponentFqn) return null;

        // Collect generic type parameters (e.g., ["T"] for ItemsRepeater<T>)
        var typeParams = new List<string>();
        if (symbol.IsGenericType)
        {
            foreach (var tp in symbol.TypeParameters)
                typeParams.Add(tp.Name);
        }

        // Find the constructor with the most parameters
        IMethodSymbol? bestCtor = null;
        foreach (var member in symbol.GetMembers())
        {
            if (member is IMethodSymbol { MethodKind: MethodKind.Constructor, DeclaredAccessibility: Accessibility.Public, IsStatic: false } ctor)
            {
                if (bestCtor is null || ctor.Parameters.Length > bestCtor.Parameters.Length)
                    bestCtor = ctor;
            }
        }

        var parameters = new List<ComponentParamInfo>();
        if (bestCtor is not null)
        {
            foreach (var p in bestCtor.Parameters)
            {
                parameters.Add(new ComponentParamInfo(
                    Name: p.Name,
                    TypeFqn: p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    IsValueType: p.Type.IsValueType,
                    HasDefaultValue: p.HasExplicitDefaultValue,
                    DefaultValue: p.HasExplicitDefaultValue ? FormatDefaultValue(p.ExplicitDefaultValue, p.Type) : null
                ));
            }
        }

        return new ComponentTypeInfo(
            FullName: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ShortName: symbol.Name,
            Namespace: symbol.ContainingNamespace.ToDisplayString(),
            TypeParams: typeParams,
            Parameters: parameters
        );
    }

    private static string? FormatDefaultValue(object? value, ITypeSymbol type)
    {
        if (value is null) return type.IsValueType ? $"default({type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})" : "null";
        if (value is string s) return $"\"{s.Replace("\"", "\\\"")}\"";
        if (value is bool b) return b ? "true" : "false";
        if (value is char c) return $"'{c}'";
        return value.ToString();
    }

    private static string CamelCase(string name)
        => name.Length > 0 ? char.ToLowerInvariant(name[0]) + name[1..] : name;

    private static string PascalCase(string name)
        => name.Length > 0 ? char.ToUpperInvariant(name[0]) + name[1..] : name;

    /// <summary>Returns true if the type is already nullable (ends with ? or is Nullable&lt;T&gt;).</summary>
    private static bool IsAlreadyNullable(string fqn)
    {
        if (fqn.EndsWith("?")) return true;
        if (fqn.StartsWith("System.Nullable<") && fqn.EndsWith(">")) return true;
        return false;
    }

    private void GenerateComponentClasses(SourceProductionContext spc, System.Collections.Immutable.ImmutableArray<ComponentTypeInfo?> types)
    {
        var sbTypes = new IndentedStringBuilder();
        sbTypes.AppendLine("// <auto-generated />");
        sbTypes.AppendLine("#nullable enable");

        var nonNull = new List<ComponentTypeInfo>();
        for (int i = 0; i < types.Length; i++)
            if (types[i] is { } t) nonNull.Add(t);

        // Use the first component's namespace as the factory class namespace
        var factoryNamespace = nonNull.Count > 0 ? nonNull[0].Namespace : "NFMWorld.Reactor";

        var sbFactories = new IndentedStringBuilder();
        sbFactories.AppendLine("// <auto-generated />");
        sbFactories.AppendLine("#nullable enable");
        // Only emit the Nodes factory class if there are component types
        if (nonNull.Count > 0)
        {
            sbFactories.AppendLine();
            sbFactories.AppendLine($"namespace {factoryNamespace}");
            sbFactories.AppendLine("{");
            using (sbFactories.Indent())
            {
                sbFactories.AppendLine("/// <summary>Unified factory methods for all Component-based VNodes in this project.</summary>");
                sbFactories.AppendLine("public static partial class Nodes");
                sbFactories.AppendLine("{");
                using (sbFactories.Indent())
                {
                    foreach (var type in nonNull)
                    {
                        GenerateComponentFactoryMethod(sbFactories, type);
                    }
                }
                sbFactories.AppendLine("}");
            }
            sbFactories.AppendLine("}");
        }

        // Generate typed ComponentNode subclasses in their origin namespaces
        foreach (var type in nonNull)
        {
            GenerateComponentNodeClass(sbTypes, type);
        }

        spc.AddSource("Components.Types.g.cs", sbTypes.ToString());
        spc.AddSource("Components.g.cs", sbFactories.ToString());
    }

    private static void GenerateComponentNodeClass(IndentedStringBuilder sb, ComponentTypeInfo type)
    {
        var nodeName = type.ShortName + "Node";
        var genericNodeName = type.IsGeneric ? $"{nodeName}{type.TypeParamsStr}" : nodeName;
        var genericTypeName = type.GenericShortName;

        sb.AppendLine();
        sb.AppendLine($"namespace {type.Namespace}");
        sb.AppendLine("{");
        using (sb.Indent())
        {
            sb.AppendLine();
            sb.AppendLine($"/// <summary>Typed VNode for component <see cref=\"{genericTypeName}\"/>.</summary>");
            sb.AppendLine($"public sealed class {genericNodeName} : global::NFMWorld.Reactor.ComponentNode");
            sb.AppendLine("{");

            using (sb.Indent())
            {
                // ── Fields for each constructor parameter ────────────────
                foreach (var p in type.Parameters)
                {
                    sb.AppendLine($"private {p.TypeFqn} _{CamelCase(p.Name)} = {GetDefaultValueExpr(p)};");
                }
                
                sb.AppendLine();
                sb.AppendLine($"public override Type ComponentType => typeof({genericTypeName});");

                sb.AppendLine();
                sb.AppendLine($"internal {nodeName}() {{ }}");

                // ── With* methods ─────────────────────────────────────────
                foreach (var p in type.Parameters)
                {
                    var pascal = PascalCase(p.Name);
                    sb.AppendLine();
                    sb.AppendLine($"/// <summary>Sets the <c>{pascal}</c> constructor argument.</summary>");
                    sb.AppendLine($"public {genericNodeName} With{pascal}({p.TypeFqn} value)");
                    sb.AppendLine("{");
                    using (sb.Indent())
                    {
                        sb.AppendLine($"_{CamelCase(p.Name)} = value;");
                        sb.AppendLine("return this;");
                    }

                    sb.AppendLine("}");
                }

                // ── Collect parameter-based With* names to avoid shadow conflicts ──
                var paramWithNames = new HashSet<string>();
                foreach (var p in type.Parameters)
                    paramWithNames.Add("With" + PascalCase(p.Name));

                // ── Shadow: only WithKey (components have no native node for Name/Children/Classes) ──
                if (!paramWithNames.Contains("WithKey"))
                    sb.AppendLine($"public new {genericNodeName} WithKey(object? key) {{ base.WithKey(key); return this; }}");

                // ── CreateComponent override ──────────────────────────────
                sb.AppendLine();
                sb.AppendLine("/// <inheritdoc />");
                sb.AppendLine("public override global::NFMWorld.Reactor.Component CreateComponent()");
                sb.AppendLine("{");

                using (sb.Indent())
                {
                    sb.AppendLine($"return new {genericTypeName}(");

                    using (sb.Indent())
                    {
                        for (int i = 0; i < type.Parameters.Count; i++)
                        {
                            var p = type.Parameters[i];
                            var camel = CamelCase(p.Name);
                            var comma = i < type.Parameters.Count - 1 ? "," : "";
                            sb.AppendLine($"_{camel}{comma}");
                        }
                    }

                    sb.AppendLine(");");
                }

                sb.AppendLine("}");

                // ── InputsEqual override ────────────────────────────────────
                sb.AppendLine();
                sb.AppendLine("/// <inheritdoc />");
                sb.AppendLine("public override bool InputsEqual(global::NFMWorld.Reactor.ComponentNode otherNode)");
                sb.AppendLine("{");
                using (sb.Indent())
                {
                    if (type.Parameters.Count == 0)
                    {
                        sb.AppendLine($"return otherNode.GetType() == typeof({genericNodeName});");
                    }
                    else
                    {
                        sb.AppendLine($"if (otherNode is not {genericNodeName} v) return false;");
                        sb.AppendLine($"if (otherNode.GetType() != typeof({genericNodeName})) return false;");
                        
                        for (int i = 0; i < type.Parameters.Count; i++)
                        {
                            var p = type.Parameters[i];
                            sb.AppendLine($"if (!global::System.Collections.Generic.EqualityComparer<{p.TypeFqn}>.Default.Equals(_{CamelCase(p.Name)}, v._{CamelCase(p.Name)})) return false;");
                        }
                        
                        sb.AppendLine("return true;");
                    }
                }
                sb.AppendLine("}");
                
                // ── GetInputs override ────────────────────────────────────
                sb.AppendLine();
                sb.AppendLine("/// <inheritdoc />");
                sb.AppendLine("public override object?[] GetInputs() =>");
                using (sb.Indent())
                {
                    if (type.Parameters.Count == 0)
                    {
                        sb.AppendLine("[];");
                    }
                    else
                    {
                        sb.AppendLine("[");
                        using (sb.Indent())
                        {
                            for (int i = 0; i < type.Parameters.Count; i++)
                            {
                                var p = type.Parameters[i];
                                var comma = i < type.Parameters.Count - 1 ? "," : "";
                                sb.AppendLine($"_{CamelCase(p.Name)}{comma}");
                            }
                        }
                        sb.AppendLine("];");
                    }
                }
            }

            sb.AppendLine("}");
        }
        sb.AppendLine("}");
    }

    private static string GetDefaultValueExpr(ComponentParamInfo p)
    {
        if (p is { HasDefaultValue: true, DefaultValue: not null })
            return p.DefaultValue;
        return p.IsValueType ? $"default({p.TypeFqn})" : "default!";
    }

    private static void GenerateComponentFactoryMethod(IndentedStringBuilder sb, ComponentTypeInfo type)
    {
        var nodeName = type.ShortName + "Node";
        var genericNodeName = type.IsGeneric ? $"{nodeName}{type.TypeParamsStr}" : nodeName;
        var returnType = $"global::{type.Namespace}.{genericNodeName}";
        var factorySig = type.GenericShortName;

        sb.AppendLine();
        sb.AppendLine($"/// <summary>Create a <see cref=\"{type.GenericShortName}\"/> component VNode.</summary>");
        sb.Append($"public static {returnType} {factorySig}(");

        var paramDecls = new List<string>();
        foreach (var p in type.Parameters)
        {
            if (p.HasDefaultValue)
                paramDecls.Add($"{p.TypeFqn} {CamelCase(p.Name)} = {GetDefaultValueExpr(p)}");
            else
                paramDecls.Add($"{p.TypeFqn} {CamelCase(p.Name)}");
        }

        sb.AppendLine();
        using (sb.Indent())
        {
            for (int i = 0; i < paramDecls.Count; i++)
            {
                sb.Append(paramDecls[i]);
                if (i < paramDecls.Count - 1)
                    sb.AppendLine(",");
            }
        }

        sb.AppendLine(")");
        sb.AppendLine("{");
        using (sb.Indent())
        {
            sb.AppendLine($"var n = new {returnType}();");
            sb.AppendLine();
            foreach (var p in type.Parameters)
            {
                var camel = CamelCase(p.Name);
                var pascal = PascalCase(p.Name);
                sb.AppendLine($"n.With{pascal}({camel});");
            }
            sb.AppendLine();
            sb.AppendLine("return n;");
        }
        sb.AppendLine("}");
    }

    private readonly record struct ComponentTypeInfo(
        string FullName,
        string ShortName,
        string Namespace,
        List<string> TypeParams,
        List<ComponentParamInfo> Parameters)
    {
        public readonly bool IsGeneric => TypeParams.Count > 0;
        public readonly string TypeParamsStr => IsGeneric ? $"<{string.Join(", ", TypeParams)}>" : "";
        public readonly string GenericShortName => ShortName + TypeParamsStr;
    }

    private readonly record struct ComponentParamInfo(
        string Name,
        string TypeFqn,
        bool IsValueType,
        bool HasDefaultValue,
        string? DefaultValue);
}
