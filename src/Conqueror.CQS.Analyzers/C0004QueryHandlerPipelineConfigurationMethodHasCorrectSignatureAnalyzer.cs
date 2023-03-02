using System.Collections.Immutable;
using System.Linq;
using Conqueror.CQS.Analyzers.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Conqueror.CQS.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class C0004QueryHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ConquerorCQS0004";

    private const string Category = "Design";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.Analyzer0004Title), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.Analyzer0004MessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.Analyzer0004Description), Resources.ResourceManager, typeof(Resources));
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, true, Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(CheckMethodSignature, SyntaxKind.MethodDeclaration);
    }

    private static void CheckMethodSignature(SyntaxNodeAnalysisContext context)
    {
        var methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);

        if (methodSymbol == null || methodSymbol.Name != Constants.ConfigurePipelineMethodName)
        {
            return;
        }

        if (IsValidPipelineConfigurationMethod(methodSymbol))
        {
            return;
        }

        if (methodDeclarationSyntax.Parent is ClassDeclarationSyntax classDeclarationSyntax)
        {
            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

            // if the containing class is not a query handler, we ignore the method
            if (!classSymbol.IsQueryHandlerType(context) || !classSymbol.HasConfigureQueryPipelineInterface(context))
            {
                return;
            }

            // if the containing class has a valid configuration method, we ignore any invalid configuration methods, since they could be overloads etc.
            if (classDeclarationSyntax.Members.OfType<MethodDeclarationSyntax>().Select(m => context.SemanticModel.GetDeclaredSymbol(m)).Any(IsValidPipelineConfigurationMethod))
            {
                return;
            }
        }

        var diagnostic = Diagnostic.Create(Rule, methodSymbol.Locations[0], methodSymbol.Name);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsValidPipelineConfigurationMethod(IMethodSymbol symbol)
    {
        return symbol != null &&
               symbol.ReturnsVoid &&
               symbol.IsStatic &&
               symbol.TypeParameters.IsEmpty &&
               symbol.Parameters.Length == 1 &&
               symbol.Parameters[0].Type.MetadataName == Constants.QueryPipelineBuilderInterfaceName &&
               symbol.Parameters[0].Type.ContainingAssembly.Name == Constants.AbstractionsAssemblyName;
    }
}
