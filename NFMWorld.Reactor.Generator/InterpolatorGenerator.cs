using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using WorldXaml.Generator.Common;

namespace WorldXaml.Generator;

[Generator(LanguageNames.CSharp)]
public class InterpolatorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all methods annotated with [XamlInterpolator] attribute
        var interpolatorMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "WorldXaml.UI.Base.XamlInterpolatorAttribute",
                static (node, _) => node is MethodDeclarationSyntax,
                static (context, _) =>
                {
                    var syntax = (MethodDeclarationSyntax)context.TargetNode;
                    var method = context.SemanticModel.GetDeclaredSymbol(syntax)!;

                    return (
                        ContainingType: method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        MethodName: method.Name,
                        InterpolatedType: method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    );
                })
            .WithTrackingName("XamlInterpolatorMethodsProvider");

        // Registers them into the InterpolatorRegistry
        context.RegisterSourceOutput(interpolatorMethods.Collect(), static (context, attrs) =>
        {
            var sb = new IndentedStringBuilder();
            sb.AppendLine("#nullable enable");

            sb.AppendLine("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");;
            sb.AppendLine("internal static class __InterpolatorHook");
            sb.AppendLine("{");
            using (sb.Indent())
            {
                sb.AppendLine("[System.Runtime.CompilerServices.ModuleInitializerAttribute]");
                sb.AppendLine("public static void Init()");
                sb.AppendLine("{");
                using (sb.Indent())
                {
                    foreach (var (containingType, methodName, interpolatedType) in attrs)
                    {
                        sb.AppendLine($"global::WorldXaml.UI.Base.InterpolatorRegistry.RegisterInterpolator<{interpolatedType}>({containingType}.{methodName});");
                    }
                }
                sb.AppendLine("}");
            }
            sb.AppendLine("}");
            
            context.AddSource("__InterpolatorHook.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        });
    }
}