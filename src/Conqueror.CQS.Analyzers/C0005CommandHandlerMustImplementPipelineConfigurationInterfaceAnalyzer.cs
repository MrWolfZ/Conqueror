using System.Collections.Immutable;
using Conqueror.CQS.Analyzers.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Conqueror.CQS.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class C0005CommandHandlerMustImplementPipelineConfigurationInterfaceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ConquerorCQS0005";

        private const string Category = "Design";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.Analyzer0005Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.Analyzer0005MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.Analyzer0005Description), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Hidden, true, Description);

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

            if (!symbol.IsCommandHandlerType(context) || symbol.HasConfigureCommandPipelineInterface(context))
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, symbol?.Locations[0], symbol?.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
