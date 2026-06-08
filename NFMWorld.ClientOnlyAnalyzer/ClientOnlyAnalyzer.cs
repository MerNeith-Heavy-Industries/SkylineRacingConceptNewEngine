using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NFMWorld.ClientOnlyAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ClientOnlyAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NFMW0001";

    private const string Category = "Usage";

    private const string ClientOnlyAttributeFullName = "NFMWorldLibrary.Backend.Gamemodes.ClientOnlyAttribute";
    private const string ClientServerTypeFullName = "NFMWorldLibrary.Backend.Gamemodes.ClientServer";
    private const string RunIfOnClientMethodName = "RunIfOnClient";

    /// <summary>
    /// The name of the NFMWorld executable project assembly.
    /// </summary>
    private const string NfmWorldAssemblyName = "NFMWorld";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Client-only method called outside NFMWorld or RunIfOnClient",
        messageFormat: "Method '{0}' is marked [ClientOnly] and can only be called from the NFMWorld assembly or via ClientServer.RunIfOnClient",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Methods annotated with [ClientOnly] must only be invoked from within the NFMWorld project or passed as a delegate to ClientServer.RunIfOnClient.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Resolve the invoked method symbol.
        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
            is not IMethodSymbol methodSymbol)
        {
            return;
        }

        // Early exit: if the method does not have [ClientOnly], skip.
        if (!HasClientOnlyAttribute(methodSymbol))
        {
            return;
        }

        // Allowed: the call site is inside the NFMWorld executable assembly.
        if (context.Compilation.Assembly.Name == NfmWorldAssemblyName)
        {
            return;
        }

        // Allowed: the invocation is nested inside a delegate passed to ClientServer.RunIfOnClient.
        if (IsInsideRunIfOnClientArgument(invocation, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        // Allowed: the containing method is itself [ClientOnly] or overrides a
        // [ClientOnly] method (i.e. we're in the "client-only chain").
        if (IsInsideClientOnlyChain(invocation, context.SemanticModel, context.CancellationToken))
        {
            return;
        }

        // Not allowed — report diagnostic.
        var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), methodSymbol.Name);
        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Checks whether the method symbol has the <c>[ClientOnly]</c> attribute.
    /// Checks the method itself, its <c>OriginalDefinition</c>, overridden
    /// methods, explicitly implemented interface methods, and — for methods
    /// resolved on a class that inherits a default interface method — the
    /// corresponding interface method declaration.
    /// </summary>
    private static bool HasClientOnlyAttribute(IMethodSymbol methodSymbol)
    {
        // Check the method symbol itself.
        if (HasAttributeOnSymbol(methodSymbol))
        {
            return true;
        }

        // Check the original definition (e.g., for overrides or base. calls
        // Roslyn may resolve to a symbol that strips attributes).
        if (methodSymbol.OriginalDefinition is { } original
            && !SymbolEqualityComparer.Default.Equals(original, methodSymbol))
        {
            if (HasAttributeOnSymbol(original))
            {
                return true;
            }
        }

        // Check overridden method chain.
        for (var overridden = methodSymbol.OverriddenMethod;
             overridden is not null;
             overridden = overridden.OverriddenMethod)
        {
            if (HasAttributeOnSymbol(overridden))
            {
                return true;
            }
        }

        // Check explicitly implemented interface methods.
        foreach (var ifaceMethod in methodSymbol.ExplicitInterfaceImplementations)
        {
            if (HasAttributeOnSymbol(ifaceMethod))
            {
                return true;
            }
        }

        // When Roslyn resolves a base.X() call for a default interface method,
        // it synthesises a method on the base class that does not carry the
        // original [ClientOnly] attribute. Walk all interfaces of the
        // containing type and check matching methods.
        if (methodSymbol.ContainingType is { } containingType)
        {
            foreach (var iface in containingType.AllInterfaces)
            {
                foreach (var ifaceMember in iface.GetMembers(methodSymbol.Name))
                {
                    if (ifaceMember is not IMethodSymbol ifaceMethod)
                    {
                        continue;
                    }

                    // Match on parameter count (the type system already
                    // resolved the call, so this is sufficient).
                    if (ifaceMethod.Parameters.Length != methodSymbol.Parameters.Length)
                    {
                        continue;
                    }

                    if (HasAttributeOnSymbol(ifaceMethod))
                    {
                        return true;
                    }
                }
            }
        }

        // Also check the associated symbol for property getters/setters, etc.
        if (methodSymbol.AssociatedSymbol is { } associated)
        {
            if (HasAttributeOnSymbol(associated))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns <c>true</c> if <paramref name="symbol"/> directly bears a
    /// <c>[ClientOnly]</c> attribute.
    /// </summary>
    private static bool HasAttributeOnSymbol(ISymbol symbol)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass is { } attrClass
                && GetFullMetadataName(attrClass) == ClientOnlyAttributeFullName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Walks up the syntax tree from <paramref name="invocation"/> to determine
    /// whether the call is nested inside a method that is itself in the
    /// "client-only chain" — i.e. the containing method has <c>[ClientOnly]</c>
    /// or overrides a method that has it. Also checks local functions and
    /// anonymous functions, walking up to the nearest enclosing method.
    /// </summary>
    private static bool IsInsideClientOnlyChain(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Walk up through all enclosing function-like scopes.
        SyntaxNode? current = invocation.Parent;
        while (current is not null)
        {
            if (current is MethodDeclarationSyntax methodDecl)
            {
                var method = semanticModel.GetDeclaredSymbol(methodDecl, cancellationToken);
                if (method is not null && HasClientOnlyAttribute(method))
                {
                    return true;
                }
            }
            else if (current is LocalFunctionStatementSyntax localFunc)
            {
                if (semanticModel.GetDeclaredSymbol(localFunc, cancellationToken) is IMethodSymbol method
                    && HasClientOnlyAttribute(method))
                {
                    return true;
                }
            }
            else if (current is AnonymousFunctionExpressionSyntax)
            {
                // Anonymous functions (lambdas, delegate () {}) don't have
                // their own attributes — keep walking up to the enclosing
                // named method.
            }

            // Stop walking once we hit a type declaration — we've left
            // all method scopes.
            if (current is TypeDeclarationSyntax)
            {
                break;
            }

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Walks up the syntax tree from <paramref name="invocation"/> to determine
    /// whether the call is nested inside a delegate (lambda or anonymous method)
    /// that is passed as an argument to <c>ClientServer.RunIfOnClient</c>.
    /// </summary>
    private static bool IsInsideRunIfOnClientArgument(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Walk up from the invocation through its ancestors.
        SyntaxNode? current = invocation.Parent;

        while (current is not null)
        {
            // Stop if we hit a member declaration — we've left the local scope.
            if (current is MemberDeclarationSyntax)
            {
                return false;
            }

            // Check if the current node is an argument to an invocation.
            if (current is ArgumentSyntax argument
                && argument.Parent is ArgumentListSyntax argList
                && argList.Parent is InvocationExpressionSyntax outerInvocation)
            {
                if (IsRunIfOnClientInvocation(outerInvocation, semanticModel, cancellationToken))
                {
                    return true;
                }
            }

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Determines whether <paramref name="invocation"/> is a call to
    /// <c>ClientServer.RunIfOnClient</c> (either overload).
    /// </summary>
    private static bool IsRunIfOnClientInvocation(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol
            is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        return methodSymbol.Name == RunIfOnClientMethodName
               && methodSymbol.ContainingType is { } containingType
               && GetFullMetadataName(containingType) == ClientServerTypeFullName;
    }

    /// <summary>
    /// Builds the full metadata name (namespace + type name) for a type symbol,
    /// recursing through nested types.
    /// </summary>
    private static string GetFullMetadataName(INamedTypeSymbol type)
    {
        var parts = new List<string>();
        BuildName(type, parts);
        return string.Join(".", parts);

        static void BuildName(INamedTypeSymbol t, List<string> list)
        {
            if (t.ContainingType is { } outer)
            {
                BuildName(outer, list);
                list.Add(t.Name);
            }
            else
            {
                if (t.ContainingNamespace is { IsGlobalNamespace: false } ns)
                {
                    list.Add(ns.ToDisplayString());
                }
                list.Add(t.Name);
            }
        }
    }
}
