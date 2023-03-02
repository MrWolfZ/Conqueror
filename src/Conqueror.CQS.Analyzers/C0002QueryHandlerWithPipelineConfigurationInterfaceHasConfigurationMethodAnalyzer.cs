using System.Collections.Immutable;
using System.Linq;
using Conqueror.CQS.Analyzers.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Conqueror.CQS.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class C0002QueryHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ConquerorCQS0002";

    private const string Category = "Design";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.Analyzer0002Title), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.Analyzer0002MessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.Analyzer0002Description), Resources.ResourceManager, typeof(Resources));
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, true, Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(CheckClassForInterfaces, SyntaxKind.ClassDeclaration);
    }

    private static void CheckClassForInterfaces(SyntaxNodeAnalysisContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

        if (!symbol.IsQueryHandlerType(context) || !symbol.HasConfigureQueryPipelineInterface(context))
        {
            return;
        }

        var hasConfigurePipelineMethod = classDeclarationSyntax.Members
                                                               .OfType<MethodDeclarationSyntax>()
                                                               .Select(s => context.SemanticModel.GetDeclaredSymbol(s))
                                                               .Any(s => s?.Name == Constants.ConfigurePipelineMethodName);

        if (hasConfigurePipelineMethod)
        {
            return;
        }

        var diagnostic = Diagnostic.Create(Rule, symbol?.Locations[0], symbol?.Name);

        context.ReportDiagnostic(diagnostic);
    }
}
