using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Conqueror.CQS.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ConquerorCQS0001";

        private const string Category = "Design";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.Analyzer0001Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.Analyzer0001MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.Analyzer0001Description), Resources.ResourceManager, typeof(Resources));
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
            var declaredSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

            if (!IsCommandHandlerType(context, declaredSymbol) || !HasConfigurePipelineInterface(context, declaredSymbol))
            {
                return;
            }

            var hasConfigurePipelineMethod = classDeclarationSyntax.Members
                                                                   .OfType<MethodDeclarationSyntax>()
                                                                   .Select(s => context.SemanticModel.GetDeclaredSymbol(s))
                                                                   .Any(s => s?.Name == "ConfigurePipeline");

            if (hasConfigurePipelineMethod)
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, declaredSymbol?.Locations[0], declaredSymbol?.Name);

            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsCommandHandlerType(SyntaxNodeAnalysisContext context, INamedTypeSymbol classDeclarationSymbol)
        {
            return classDeclarationSymbol?.Interfaces.Any(i => IsCommandHandlerInterfaceType(context, i)) ?? false;
        }

        private static bool IsCommandHandlerInterfaceType(SyntaxNodeAnalysisContext context, INamedTypeSymbol interfaceTypeSymbol)
        {
            var commandHandlerWithoutResponseInterfaceType = context.Compilation.GetTypeByMetadataName("Conqueror.ICommandHandler`1");
            var commandHandlerInterfaceType = context.Compilation.GetTypeByMetadataName("Conqueror.ICommandHandler`2");

            if (commandHandlerInterfaceType == null || commandHandlerWithoutResponseInterfaceType == null)
            {
                return false;
            }
            
            if (AreEquivalent(interfaceTypeSymbol, commandHandlerInterfaceType) || AreEquivalent(interfaceTypeSymbol, commandHandlerWithoutResponseInterfaceType))
            {
                return true;
            }
            
            var declaredTypeSymbol = context.Compilation.GetTypeByMetadataName(interfaceTypeSymbol.ToString());

            return IsCommandHandlerType(context, declaredTypeSymbol);
        }

        private static bool HasConfigurePipelineInterface(SyntaxNodeAnalysisContext context, INamedTypeSymbol classDeclarationSymbol)
        {
            return classDeclarationSymbol?.Interfaces.Any(i => IsConfigurePipelineInterfaceType(context, i)) ?? false;
        }

        private static bool IsConfigurePipelineInterfaceType(SyntaxNodeAnalysisContext context, INamedTypeSymbol interfaceTypeSymbol)
        {
            var interfaceType = context.Compilation.GetTypeByMetadataName("Conqueror.IConfigureCommandPipeline");

            if (interfaceType == null)
            {
                return false;
            }
            
            if (AreEquivalent(interfaceTypeSymbol, interfaceType))
            {
                return true;
            }
            
            var declaredTypeSymbol = context.Compilation.GetTypeByMetadataName(interfaceTypeSymbol.ToString());

            return IsCommandHandlerType(context, declaredTypeSymbol);
        }

        private static bool AreEquivalent(INamedTypeSymbol symbol1, INamedTypeSymbol symbol2)
        {
            return symbol1.MetadataName == symbol2.MetadataName && symbol1.ContainingAssembly.Name == symbol2.ContainingAssembly.Name;
        }
    }
}
