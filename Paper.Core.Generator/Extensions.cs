using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Paper.Core.Generator;

internal static class Extensions
{
    public const string TargetInterfaceName = "IComponentBase";

    public static bool IsOrExtendsIComponentBase(this INamedTypeSymbol symbol)
    {
        if (symbol.IsIComponentBase())
            return true;
        foreach (var @interface in symbol.Interfaces)
        {
            if (@interface.IsIComponentBase())
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsIComponentBase(this INamedTypeSymbol symbol) => symbol is
    {
        Name: TargetInterfaceName,
        ContainingNamespace:
        {
            Name: "Components",
            ContainingNamespace:
            {
                Name: "Frent",
                ContainingNamespace.IsGlobalNamespace: true
            }
        }
    };

    public static bool IsPartial(this INamedTypeSymbol namedTypeSymbol)
    {
        return namedTypeSymbol.DeclaringSyntaxReferences
            .Select(syntaxRef => syntaxRef.GetSyntax() as TypeDeclarationSyntax)
            .Any(syntax => syntax?.Modifiers.Any(SyntaxKind.PartialKeyword) ?? false);
    }
}
