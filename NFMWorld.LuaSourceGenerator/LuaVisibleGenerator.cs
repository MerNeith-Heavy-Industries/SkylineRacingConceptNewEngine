using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NFMWorld.LuaSourceGenerator;

[Generator(LanguageNames.CSharp)]
public partial class LuaVisibleGenerator : IIncrementalGenerator
{
    public const string LuaVisibleAttrName = "nfm_world_library.Lua.LuaVisibleAttribute";
    public const string LuaNameAttrName = "nfm_world_library.Lua.LuaNameAttribute";
    public const string LuaHiddenAttrName = "nfm_world_library.Lua.LuaHiddenAttribute";
    public const string MemberLuaVisibleAttrName = "nfm_world_library.Lua.MemberLuaVisibleAttribute";
    public const string InlineArrayAttrName = "System.Runtime.CompilerServices.InlineArrayAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var symbolReferences = context.CompilationProvider
            .Select((compilation, token) => SymbolReferences.Create(compilation))
            .WithTrackingName("SymbolReferences");

        var asmName = context.CompilationProvider
            .Select((compilation, token) => compilation.AssemblyName)
            .WithTrackingName("AsmName");

        var typeProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            LuaVisibleAttrName,
            static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax or StructDeclarationSyntax or InterfaceDeclarationSyntax or EnumDeclarationSyntax,
            static (ctx, ct) => (INamedTypeSymbol)ctx.TargetSymbol)
            .WithTrackingName("LuaVisibleTypes");

        var luaTypeMetadatas = typeProvider.Combine(symbolReferences)
            .Select((pair, ct) =>
            {
                var (symbol, references) = pair;
                if (references == null) return null;
                return new LuaTypeMetadata(symbol, references);
            })
            .Where(tm => tm?.IsCandidate == true)
            .WithTrackingName("LuaTypeMetadatas");

        // Read optional stubs output directory from MSBuild property
        var stubsOutputDir = context.AnalyzerConfigOptionsProvider
            .Select((configOptions, token) =>
            {
                if (configOptions.GlobalOptions.TryGetValue(
                        "build_property.LuaVisibleGenerator_StubsOutputDirectory",
                        out var path))
                    return path;
                return null;
            })
            .WithTrackingName("StubsOutputDir");

        var typeProvider2 = context.SyntaxProvider.ForAttributeWithMetadataName(
            MemberLuaVisibleAttrName,
            static (node, _) => node is PropertyDeclarationSyntax or FieldDeclarationSyntax or MethodDeclarationSyntax or ConstructorDeclarationSyntax,
            static (ctx, ct) => ctx.TargetSymbol.ContainingType)
            .WithTrackingName("MemberLuaVisibleTypes");

        var luaTypeMetadatas2 = typeProvider2.Combine(symbolReferences)
            .Select((pair, ct) =>
            {
                var (symbol, references) = pair;
                if (references == null) return null;
                return new LuaTypeMetadata(symbol, references);
            })
            .Where(tm => tm?.IsCandidate == true)
            .WithTrackingName("MemberLuaVisibleTypeMetadatas");

        var assemblyLuaVisibleTypes = context.CompilationProvider
            .SelectMany((compilation, ct) => compilation.Assembly.GetAttributes())
            .Select((attr, ct) =>
            {
                if (attr.AttributeClass == null) return null;
                var attrName = attr.AttributeClass.ToDisplayString();
                // Match AssemblyLuaVisibleAttribute<T> (generic) or AssemblyLuaVisibleAttribute (non-generic)
                if (!attrName.StartsWith("nfm_world_library.Lua.AssemblyLuaVisibleAttribute")) return null;

                ITypeSymbol? typeSymbol = null;

                // Generic version: AssemblyLuaVisibleAttribute<T> — type is in TypeArguments
                if (attr.AttributeClass.TypeArguments.Length == 1)
                {
                    typeSymbol = attr.AttributeClass.TypeArguments[0];
                }
                // Non-generic version: AssemblyLuaVisibleAttribute(Type) — type is in constructor args
                else if (attr.ConstructorArguments.Length == 1)
                {
                    typeSymbol = attr.ConstructorArguments[0].Value as ITypeSymbol;
                }

                return typeSymbol as INamedTypeSymbol;
            })
            .Where(ts => ts != null)
            .WithTrackingName("AssemblyLuaVisibleTypes");

        var assemblyLuaTypeMetadatas = assemblyLuaVisibleTypes.Combine(symbolReferences)
            .Select((pair, ct) =>
            {
                var (symbol, references) = pair;
                if (references == null) return null;
                return new LuaTypeMetadata(symbol!, references);
            })
            .Where(tm => tm?.IsCandidate == true)
            .WithTrackingName("AssemblyLuaTypeMetadatas");

        var combined = luaTypeMetadatas.Collect().Combine(luaTypeMetadatas2.Collect()).Combine(assemblyLuaTypeMetadatas.Collect()).Combine(stubsOutputDir).Combine(asmName);

        context.RegisterSourceOutput(
            combined,
            (spc, pairs) =>
            {
                var ((((visible, memberVisible), assemblyVisible), stubsOutputDir), asmName) = pairs;

                var list = new Dictionary<string, LuaTypeMetadata>();
                foreach (var meta in visible)
                {
                    if (!list.ContainsKey(meta!.FullTypeName))
                        list[meta.FullTypeName] = meta;
                }
                foreach (var meta in assemblyVisible)
                {
                    if (!list.ContainsKey(meta!.FullTypeName))
                        list[meta.FullTypeName] = meta;
                }
                foreach (var meta in memberVisible)
                {
                    if (!list.ContainsKey(meta!.FullTypeName))
                        list[meta.FullTypeName] = meta;
                }

                var ns = $"NFMWorld.LuaSourceGenerator.Generator.{asmName}";

                foreach (var type in list.Values)
                {
                    if (type.IsEnum)
                    {
                        var generator = new LuaBindingEnumTypeGenerator(type, ns);
                        var code = generator.GenerateCode();
                        spc.AddSource($"{type.SanitizedTypeName}.cs", code);
                    }
                    else
                    {
                        var generator = new LuaBindingTypeGenerator(type, ns);
                        var code = generator.GenerateCode();
                        spc.AddSource($"{type.SanitizedTypeName}.cs", code);
                    }
                }
                {
                    var initGenerator = new LuaBindingInitGenerator(list.Values.ToArray(), ns);
                    var code = initGenerator.GenerateCode();
                    spc.AddSource("_init.cs", code);
                }

                if (stubsOutputDir != null)
                {
                    var codes = new StringBuilder();
                    foreach (var type in list.Values)
                    {
                        var initGenerator = new LuaStubsGenerator(type);
                        var code = initGenerator.GenerateCode();
                        codes.AppendLine(code);
                    }
#pragma warning disable RS1035
                    File.WriteAllText(Path.Combine(stubsOutputDir ?? "", asmName + ".lua"), codes.ToString());
#pragma warning restore RS1035

                    codes.Clear();
                    foreach (var type in OrderLuauTypes(list))
                    {
                        var initGenerator = new LuauStubsGenerator(type);
                        var code = initGenerator.GenerateCode();
                        codes.AppendLine(code);
                    }
#pragma warning disable RS1035
                    File.WriteAllText(Path.Combine(stubsOutputDir ?? "", asmName + ".d.luau"), codes.ToString());
#pragma warning restore RS1035
                }
            });
    }

    /// <summary>
    /// Orders Luau stub types so that declarations which extend another
    /// declaration are emitted after the declaration they extend. Luau
    /// resolves type aliases in declaration order, so a base type must
    /// appear before the type that extends it.
    /// </summary>
    private static List<LuaTypeMetadata> OrderLuauTypes(Dictionary<string, LuaTypeMetadata> types)
    {
        var ordered = new List<LuaTypeMetadata>(types.Count);
        var visited = new HashSet<LuaTypeMetadata>();

        void Visit(LuaTypeMetadata type)
        {
            if (!visited.Add(type))
                return;

            // Match the same dependency LuauStubsGenerator renders as `extends`:
            // a LuaVisible base type that lives in this same stub file.
            if (type.BaseType is { HasLuaVisibleAttr: true } baseType &&
                baseType.LuaName != type.LuaName &&
                types.TryGetValue(baseType.FullTypeName, out var baseMeta))
            {
                Visit(baseMeta);
            }

            ordered.Add(type);
        }

        foreach (var type in types.Values)
            Visit(type);

        return ordered;
    }
}

internal sealed class SymbolReferences
{
    public INamedTypeSymbol? LuaVisibleAttribute { get; }
    public INamedTypeSymbol? LuaNameAttribute { get; }
    public INamedTypeSymbol? LuaHiddenAttribute { get; }
    public INamedTypeSymbol? MemberLuaVisibleAttribute { get; }
    public INamedTypeSymbol? LuaShimTypeAttribute { get; }
    public INamedTypeSymbol? LuaOverloadPriorityAttribute { get; }
    public INamedTypeSymbol? InlineArrayAttribute { get; }
    public INamedTypeSymbol? ILuaUserData { get; }
    public INamedTypeSymbol? LuaTable { get; }
    public INamedTypeSymbol? LuaValue { get; }
    public INamedTypeSymbol? LuaThread { get; }
    public INamedTypeSymbol? LuaFunction { get; }
    public INamedTypeSymbol? Fixed64Vector3 { get; }
    public INamedTypeSymbol? Fixed64 { get; }
    public INamedTypeSymbol? Fixed64AngleSingle { get; }
    public INamedTypeSymbol? Fixed64Euler { get; }
    public INamedTypeSymbol IEnumerableT { get; }
    public INamedTypeSymbol? KeyValuePair { get; }

    private SymbolReferences(Compilation compilation)
    {
        LuaVisibleAttribute = compilation.GetTypeByMetadataName("nfm_world_library.Lua.LuaVisibleAttribute");
        LuaNameAttribute = compilation.GetTypeByMetadataName("nfm_world_library.Lua.LuaNameAttribute");
        LuaHiddenAttribute = compilation.GetTypeByMetadataName("nfm_world_library.Lua.LuaHiddenAttribute");
        MemberLuaVisibleAttribute = compilation.GetTypeByMetadataName("nfm_world_library.Lua.MemberLuaVisibleAttribute");
        LuaShimTypeAttribute = compilation.GetTypeByMetadataName("nfm_world_library.Lua.LuaShimTypeAttribute");
        LuaOverloadPriorityAttribute = compilation.GetTypeByMetadataName("nfm_world_library.Lua.LuaOverloadPriorityAttribute");
        InlineArrayAttribute = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.InlineArrayAttribute");
        ILuaUserData = compilation.GetTypeByMetadataName("Lua.ILuaUserData");
        LuaTable = compilation.GetTypeByMetadataName("Lua.LuaTable");
        LuaValue = compilation.GetTypeByMetadataName("Lua.LuaValue");
        LuaThread = compilation.GetTypeByMetadataName("Lua.LuaThread");
        LuaFunction = compilation.GetTypeByMetadataName("Lua.LuaFunction");
        Fixed64 = compilation.GetTypeByMetadataName("FixedMathSharp.Fixed64");
        Fixed64Vector3 = compilation.GetTypeByMetadataName("FixedMathSharp.Vector3d");
        Fixed64AngleSingle = compilation.GetTypeByMetadataName("NFMWorldLibrary.FixedMath.f64AngleSingle");
        Fixed64Euler = compilation.GetTypeByMetadataName("NFMWorldLibrary.FixedMath.f64Euler");

        IEnumerableT = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);
        KeyValuePair = compilation.GetTypeByMetadataName("System.Collections.Generic.KeyValuePair`2");
    }

    public static SymbolReferences? Create(Compilation compilation)
    {
        var r = new SymbolReferences(compilation);
        return r.LuaVisibleAttribute != null ? r : null;
    }
}
