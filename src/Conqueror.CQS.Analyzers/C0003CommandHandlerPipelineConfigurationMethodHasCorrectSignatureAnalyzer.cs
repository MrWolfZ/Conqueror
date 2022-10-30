using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Conqueror.CQS.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ConquerorCQS0003";

        private const string Category = "Design";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.Analyzer0003Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.Analyzer0003MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.Analyzer0003Description), Resources.ResourceManager, typeof(Resources));
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

            if (methodSymbol == null || methodSymbol.Name != "ConfigurePipeline")
            {
                return;
            }

            if (methodDeclarationSyntax.Parent is ClassDeclarationSyntax classDeclarationSyntax)
            {
                var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

                if (!IsCommandHandlerType(context, classSymbol) || !HasConfigurePipelineInterface(context, classSymbol))
                {
                    return;
                }

                if (classDeclarationSyntax.Members.OfType<MethodDeclarationSyntax>().Select(m => context.SemanticModel.GetDeclaredSymbol(m)).Any(
                        m => m != null && m.ReturnsVoid && m.IsStatic && m.TypeParameters.IsEmpty && m.Parameters.Length == 1 && m.Parameters[0].Type.MetadataName == "ICommandPipelineBuilder" &&
                             m.Parameters[0].Type.ContainingAssembly.Name == "Conqueror.CQS.Abstractions"))
                {
                    return;
                }
            }

            if (methodSymbol.ReturnsVoid && methodSymbol.IsStatic && methodSymbol.TypeParameters.IsEmpty && methodSymbol.Parameters.Length == 1 &&
                methodSymbol.Parameters[0].Type.MetadataName == "ICommandPipelineBuilder" && methodSymbol.Parameters[0].Type.ContainingAssembly.Name == "Conqueror.CQS.Abstractions")
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, methodSymbol.Locations[0], methodSymbol.Name);

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

        private static bool AreEquivalent(ISymbol symbol1, ISymbol symbol2)
        {
            return symbol1.MetadataName == symbol2.MetadataName && symbol1.ContainingAssembly.Name == symbol2.ContainingAssembly.Name;
        }
    }
}
