using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;
using System.Diagnostics;

namespace Paper.Core.Generator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class UpdateAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;
    private static readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics;

    static UpdateAttributeAnalyzer()
    {
        _supportedDiagnostics = [UpdateMethodNoAttribute];
    }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        TypeDeclarationSyntax typeDeclarationSyntax = (TypeDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax) is not INamedTypeSymbol namedTypeSymbol)
            return;

        bool isComponent = false;

        foreach (var @interface in namedTypeSymbol.AllInterfaces)
        {
            if (!@interface.IsOrExtendsIComponentBase())
                return;

            isComponent = true;

            break;
        }

        if (!isComponent)
            return;

        foreach (var item in typeDeclarationSyntax.Members)
        {
            if (item is not MethodDeclarationSyntax method || 
                method.AttributeLists.Count != 0 || 
                method.Identifier.ToString() != "Update")
                continue;

            if(context.SemanticModel.GetDeclaredSymbol(method) is { } symbol)
                Report(UpdateMethodNoAttribute, symbol, namedTypeSymbol.Name);
        }

        void Report(DiagnosticDescriptor diagnosticDescriptor, ISymbol location, params object?[] args)
        {
            context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, location.Locations.First(), args));
        }
    }

#pragma warning disable RS2008 // Enable analyzer release tracking
    public static readonly DiagnosticDescriptor UpdateMethodNoAttribute = new(
        id: "PC0000",
        title: "Update Method With No Attribute",
        messageFormat: "Update method on '{0}' should have an attribute",
        category: "Components",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
