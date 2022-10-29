using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Conqueror.CQS.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzerCodeFixProvider))]
    [Shared]
    public sealed class CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzerCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic
            var declaration = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    Resources.Analyzer0001CodeFixTitle,
                    c => ImplementConfigurationMethod(context.Document, declaration, c),
                    nameof(Resources.Analyzer0001CodeFixTitle)),
                diagnostic);
        }

        private static async Task<Solution> ImplementConfigurationMethod(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            if (root == null)
            {
                return document.Project.Solution;
            }

            var methodDeclaration = SyntaxFactory.MethodDeclaration(default,
                                                                    SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword)).Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword)),
                                                                    SyntaxFactory.ParseTypeName("void"),
                                                                    null,
                                                                    SyntaxFactory.Identifier("ConfigurePipeline"),
                                                                    null,
                                                                    SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new List<ParameterSyntax>
                                                                    {
                                                                        SyntaxFactory.Parameter(default,
                                                                                                default,
                                                                                                SyntaxFactory.ParseTypeName("ICommandPipelineBuilder"),
                                                                                                SyntaxFactory.Identifier("pipeline"),
                                                                                                null),
                                                                    })),
                                                                    default,
                                                                    SyntaxFactory.Block(),
                                                                    null)
                                                 .NormalizeWhitespace()
                                                 .WithAdditionalAnnotations(Formatter.Annotation)
                                                 .WithLeadingTrivia(SyntaxFactory.LineFeed);

            return document.WithSyntaxRoot(
                root.ReplaceNode(
                    typeDecl,
                    typeDecl.WithMembers(typeDecl.Members.Add(methodDeclaration)))).Project.Solution;
        }
    }
}
