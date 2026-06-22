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

        sb.AppendLine();
        sb.AppendLine($"namespace {type.Namespace}");
        sb.AppendLine("{");
        using (sb.Indent())
        {
            sb.AppendLine();
            sb.AppendLine($"/// <summary>Typed VNode for component <see cref=\"{type.ShortName}\"/>.</summary>");
            sb.AppendLine($"public sealed class {nodeName} : global::NFMWorld.Reactor.ComponentNode");
            sb.AppendLine("{");

            using (sb.Indent())
            {
                // ── Fields for each constructor parameter ────────────────
                foreach (var p in type.Parameters)
                {
                    sb.AppendLine($"private global::NFMWorld.Reactor.Optional<{p.TypeFqn}> _{CamelCase(p.Name)};");
                }
                
                sb.AppendLine();
                sb.AppendLine($"public override Type ComponentType => typeof({type.ShortName});");

                sb.AppendLine();
                sb.AppendLine($"internal {nodeName}() {{ }}");

                // ── With* methods ─────────────────────────────────────────
                foreach (var p in type.Parameters)
                {
                    var pascal = PascalCase(p.Name);
                    sb.AppendLine();
                    sb.AppendLine($"/// <summary>Sets the <c>{pascal}</c> constructor argument.</summary>");
                    sb.AppendLine($"public {nodeName} With{pascal}({p.TypeFqn} value)");
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
                    sb.AppendLine($"public new {nodeName} WithKey(object? key) {{ base.WithKey(key); return this; }}");

                // ── CreateComponent override ──────────────────────────────
                sb.AppendLine();
                sb.AppendLine("/// <inheritdoc />");
                sb.AppendLine("public override global::NFMWorld.Reactor.Component CreateComponent()");
                sb.AppendLine("{");

                using (sb.Indent())
                {
                    sb.AppendLine($"return new {type.ShortName}(");

                    using (sb.Indent())
                    {
                        for (int i = 0; i < type.Parameters.Count; i++)
                        {
                            var p = type.Parameters[i];
                            var camel = CamelCase(p.Name);
                            var comma = i < type.Parameters.Count - 1 ? "," : "";
                            var defaultVal = GetDefaultValueExpr(p);
                            sb.AppendLine($"_{camel}.HasValue ? _{camel}.Value : {defaultVal}{comma}");
                        }
                    }

                    sb.AppendLine(");");
                }

                sb.AppendLine("}");
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
        var returnType = $"global::{type.Namespace}.{nodeName}";

        sb.AppendLine();
        sb.AppendLine($"/// <summary>Create a <see cref=\"{type.ShortName}\"/> component VNode.</summary>");
        sb.Append($"public static {returnType} {type.ShortName}(");

        var paramDecls = new List<string>();
        foreach (var p in type.Parameters)
        {
            var paramType = $"global::NFMWorld.Reactor.Optional<{p.TypeFqn}>";
            paramDecls.Add($"{paramType} {CamelCase(p.Name)} = default");
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
                sb.AppendLine($"if ({camel}.HasValue) n.With{pascal}({camel}.Value);");
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
        List<ComponentParamInfo> Parameters);

    private readonly record struct ComponentParamInfo(
        string Name,
        string TypeFqn,
        bool IsValueType,
        bool HasDefaultValue,
        string? DefaultValue);
}
