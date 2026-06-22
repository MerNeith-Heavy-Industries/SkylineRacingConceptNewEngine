using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WorldXaml.Generator.Common;

namespace NFMWorld.Reactor.Generator;

[Generator(LanguageNames.CSharp)]
public class ReactorNodeFactoryGenerator : IIncrementalGenerator
{
    private const string PropertyAttributeFqn = "global::WorldXaml.UI.Base.PropertyAttribute";
    private const string ContentAttributeFqn = "global::WorldXaml.UI.Metadata.ContentAttribute";
    private const string VisualFqn = "global::WorldXaml.UI.Yoga.Visual";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(spc =>
            spc.AddSource("_ReactorPing.g.cs", "// ReactorNodeFactoryGenerator loaded\n"));

        // Find all non-abstract types that extend Visual from the compilation
        var nodeTypes = context.CompilationProvider
            .SelectMany((compilation, _) =>
            {
                var types = new List<TypeInfo?>();

                SearchSubtypes("WorldXaml.UI.Yoga.Visual");

                // Dedup by full type name
                var seen = new HashSet<string>();
                var deduped = new List<TypeInfo?>();
                foreach (var t1 in types)
                {
                    if (t1 is { } t && seen.Add(t.FullName))
                        deduped.Add(t);
                }

                return System.Collections.Immutable.ImmutableArray.CreateRange(deduped);

                // Find known base types and search for their non-abstract subtypes
                void SearchSubtypes(string baseTypeFqn)
                {
                    var baseType = compilation.GetTypeByMetadataName(baseTypeFqn);
                    if (baseType is null) return;

                    foreach (var asm in compilation.SourceModule.ReferencedAssemblySymbols)
                        CollectSubtypes(asm.GlobalNamespace, baseType, types);
                    CollectSubtypes(compilation.Assembly.GlobalNamespace, baseType, types);
                }
            })
            .WithTrackingName("ReactorNodeTypes");

        // Collect all types and generate the Nodes class
        context.RegisterSourceOutput(
            nodeTypes.Collect(),
            GenerateNodesClass);
    }

    private static void CollectSubtypes(INamespaceSymbol ns, INamedTypeSymbol baseType, List<TypeInfo?> results)
    {
        foreach (var member in ns.GetMembers())
        {
            if (member is INamedTypeSymbol type)
            {
                if (!type.IsAbstract && ExtendsType(type, baseType))
                {
                    var info = CollectTypeInfo(type);
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

    private static TypeInfo? CollectTypeInfo(INamedTypeSymbol symbol)
    {
        var properties = new List<PropInfo>();
        var seen = new HashSet<string>();

        // Walk the entire type hierarchy looking for instance properties
        // that have a corresponding static *Property field (convention-based detection)
        {
            var current = symbol;
            while (current is not null)
            {
                foreach (var member in current.GetMembers())
                {
                    if (member is not IPropertySymbol prop) continue;
                    if (prop.IsStatic) continue;
                    if (!seen.Add(prop.Name)) continue;

                    // Check for matching static *Property field (e.g. "FlexDirection" → "FlexDirectionProperty")
                    var backingName = prop.Name + "Property";
                    var hasBacking = current.GetMembers(backingName).Any(m => m.IsStatic)
                                     || (current.BaseType?.GetMembers(backingName).Any(m => m.IsStatic) ?? false);

                    if (!hasBacking) continue;

                    properties.Add(new PropInfo(
                        Name: prop.Name,
                        TypeFqn: prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        IsValueType: prop.Type.IsValueType,
                        HasDefaultValue: TryGetDefaultValue(prop, out var defaultVal),
                        DefaultValue: defaultVal
                    ));
                }

                current = current.BaseType;
            }
        }

        if (properties.Count == 0) return null;

        // Check for [Content] on any property member on type and parents; if found, look for Add(T)
        // on the *property's type* (e.g. NodeChildCollection.Add(Visual))
        string? childType = null;
        {
            var current = symbol;
            while (current is not null)
            {
                foreach (var member in symbol.GetMembers())
                {
                    if (member is not IPropertySymbol contentProp) continue;
                    var hasContent = member.GetAttributes().Any(a =>
                        a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == ContentAttributeFqn);
                    if (!hasContent) continue;

                    // Look for Add(T) on the property's type (e.g. NodeChildCollection)
                    childType = contentProp.Type is INamedTypeSymbol propType ? FindAddMethodChildType(propType) : null;
                    break;
                }

                current = current.BaseType;
            }
        }

        return new TypeInfo(
            FullName: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ShortName: symbol.Name,
            Namespace: symbol.ContainingNamespace.ToDisplayString(),
            Properties: properties,
            ChildType: childType
        );
    }

    /// <summary>
    /// Finds the T in an Add(T child) method on the type or its base types.
    /// Returns the fully-qualified type name, or null if not found.
    /// </summary>
    private static string? FindAddMethodChildType(INamedTypeSymbol symbol)
    {
        var current = symbol;
        while (current is not null)
        {
            foreach (var method in current.GetMembers().OfType<IMethodSymbol>())
            {
                if (method is { Name: "Add", Parameters.Length: 1, IsStatic: false, ReturnsVoid: true })
                {
                    return method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }
            current = current.BaseType;
        }
        return null;
    }

    private static bool TryGetDefaultValue(IPropertySymbol prop, out string? defaultVal)
    {
        defaultVal = null;
        foreach (var attr in prop.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) !=
                PropertyAttributeFqn) continue;

            foreach (var namedArg in attr.NamedArguments)
            {
                if (namedArg is { Key: "DefaultValue", Value.IsNull: false })
                {
                    defaultVal = namedArg.Value.ToCSharpString();
                    return true;
                }
            }
        }
        return false;
    }

    private static string CamelCase(string name)
        => name.Length > 0 ? char.ToLowerInvariant(name[0]) + name[1..] : name;

    private void GenerateNodesClass(SourceProductionContext spc, System.Collections.Immutable.ImmutableArray<TypeInfo?> types)
    {
        var sbNodes = new IndentedStringBuilder();
        sbNodes.AppendLine("// <auto-generated />");
        sbNodes.AppendLine("#nullable enable");

        // Generate typed VNode subclasses in their origin namespaces
        var sbTypes = new IndentedStringBuilder();
        sbTypes.AppendLine("// <auto-generated />");
        sbTypes.AppendLine("#nullable enable");

        var nonNull = new List<TypeInfo>();
        for (int i = 0; i < types.Length; i++)
            if (types[i] is { } t) nonNull.Add(t);

        var emittedNamespaces = new HashSet<string>();

        foreach (var type in nonNull)
        {
            sbTypes.AppendLine();
            sbTypes.AppendLine($"namespace {type.Namespace}");
            sbTypes.AppendLine("{");
            sbTypes.IncrementIndent();
                
            sbNodes.AppendLine();
            sbNodes.AppendLine($"namespace {type.Namespace}");
            sbNodes.AppendLine("{");
            sbNodes.IncrementIndent();

            // Generate factory method in Nodes class
            GenerateFactory(sbNodes, type);

            GenerateNodeClass(sbTypes, type);

            sbNodes.DecrementIndent();
            sbNodes.AppendLine("}");
            
            sbTypes.DecrementIndent();
            sbTypes.AppendLine("}");
        }

        spc.AddSource("Nodes.g.cs", sbNodes.ToString());
        spc.AddSource("Nodes.Types.g.cs", sbTypes.ToString());
    }

    private static void GenerateNodeClass(IndentedStringBuilder sb, TypeInfo type)
    {
        var nodeName = type.ShortName + "Node";
        sb.AppendLine();
        sb.AppendLine($"/// <summary>Typed VNode for <see cref=\"{type.ShortName}\"/>.</summary>");
        sb.AppendLine($"public sealed class {nodeName} : NFMWorld.Reactor.VNode");
        sb.AppendLine("{");

        using (sb.Indent())
        {
            sb.AppendLine($"internal {nodeName}() : base(typeof({type.ShortName})) {{ }}");
            sb.AppendLine();

            foreach (var prop in type.Properties)
            {
                var typeFqn = StripNullable(prop.TypeFqn);
                if (prop.IsValueType)
                    if (typeFqn != prop.TypeFqn)
                        sb.AppendLine($"public {nodeName} With{prop.Name}({prop.TypeFqn} value) => SetPropValNullable<{nodeName}, {typeFqn}>({type.ShortName}.{prop.Name}Property, value);");
                    else
                        sb.AppendLine($"public {nodeName} With{prop.Name}({prop.TypeFqn} value) => SetPropVal<{nodeName}, {prop.TypeFqn}>({type.ShortName}.{prop.Name}Property, value);");
                else
                    sb.AppendLine($"public {nodeName} With{prop.Name}({prop.TypeFqn} value) => SetProp<{nodeName}, {prop.TypeFqn}>({type.ShortName}.{prop.Name}Property, value);");
            }

            if (type.ChildType is not null)
            {
                var childNodeType = type.ChildType + "Node";
                if (type.ChildType == VisualFqn)
                {
                    childNodeType = "NFMWorld.Reactor.VNode";
                }
                sb.AppendLine();
                sb.AppendLine($"public {nodeName} WithChildren(params ReadOnlySpan<{childNodeType}> children) {{ Children ??= []; Children.AddRange(children); return this; }}");
                sb.AppendLine();
                sb.AppendLine($"public {nodeName} WithChild({childNodeType} child) {{ Children ??= []; Children.Add(child); return this; }}");
            }
        }

        sb.AppendLine("}");
    }

    private static void GenerateFactory(IndentedStringBuilder sb, TypeInfo type)
    {
        sb.AppendLine();
        sb.AppendLine($"/// <summary>Typed VNode factories for <see cref=\"{type.ShortName}\"/>.</summary>");
        sb.AppendLine($"public static class {type.ShortName}NodeFactories");
        sb.AppendLine("{");

        using (sb.Indent())
        {
            sb.AppendLine($"/// <summary>Create a <see cref=\"{type.ShortName}\"/> VNode.</summary>");
            sb.Append($"public static global::{type.Namespace}.{type.ShortName}Node {type.ShortName}(");

            var paramDecls = new List<string>();
            foreach (var prop in type.Properties)
            {
                var camelName = CamelCase(prop.Name);
                var paramType = $"NFMWorld.Reactor.Optional<{prop.TypeFqn}>";
                paramDecls.Add($"{paramType} {camelName} = default");
            }

            if (type.ChildType is not null)
            {
                var childNodeType = type.ChildType + "Node";
                if (type.ChildType == VisualFqn)
                {
                    childNodeType = "NFMWorld.Reactor.VNode";
                }
                paramDecls.Add($"params ReadOnlySpan<{childNodeType}> children");
            }

            using (sb.Indent())
            {
                var first = true;
                foreach (var param in paramDecls)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.AppendLine(", ");
                    }
                    sb.Append(param);
                }
            }

            sb.AppendLine(")");
            sb.AppendLine("{");

            using (sb.Indent())
            {
                sb.AppendLine($"var n = new {type.Namespace}.{type.ShortName}Node();");
                sb.AppendLine();

                foreach (var prop in type.Properties)
                {
                    var camelName = CamelCase(prop.Name);
                    sb.AppendLine($"if ({camelName}.HasValue) n.With{prop.Name}({camelName}.Value);");
                }

                if (type.ChildType is not null)
                    sb.AppendLine($"if (children.Length > 0) n.WithChildren(children);");

                sb.AppendLine();
                sb.AppendLine("return n;");
            }

            sb.AppendLine("}");
        }

        sb.AppendLine("}");
    }

    /// <summary>Strips Nullable&lt;T&gt; wrappers, returning T. E.g. "System.Nullable&lt;float&gt;" → "float".</summary>
    private static string StripNullable(string fqn)
    {
        if (fqn.StartsWith("System.Nullable<") && fqn.EndsWith(">"))
            return fqn.Substring(18, fqn.Length - 19); // Strip "System.Nullable<" and ">"
        if (fqn.EndsWith("?"))
            return fqn[..^1];
        return fqn;
    }

    /// <summary>Simplifies a globally-qualified type name relative to a namespace.</summary>
    private static string SimplifyType(string fqn, string ns)
    {
        fqn = StripNullable(fqn);
        var nsPrefix = $"global::{ns}.";
        if (fqn.StartsWith(nsPrefix))
            return fqn.Substring(nsPrefix.Length);
        return fqn;
    }

    private readonly record struct TypeInfo(
        string FullName,
        string ShortName,
        string Namespace,
        List<PropInfo> Properties,
        string? ChildType);

    private readonly record struct PropInfo(
        string Name,
        string TypeFqn,
        bool IsValueType,
        bool HasDefaultValue,
        string? DefaultValue);
}
