using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.CQS.Analyzers.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Conqueror.CQS.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzerCodeFixProvider))]
    [Shared]
    public sealed class C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzerCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(C0003CommandHandlerPipelineConfigurationMethodHasCorrectSignatureAnalyzer.DiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the method declaration identified by the diagnostic
            var declaration = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    Resources.Analyzer0003CodeFixTitle,
                    c => FixConfigurationMethodSignature(context.Document, declaration, c),
                    nameof(Resources.Analyzer0003CodeFixTitle)),
                diagnostic);
        }

        private static async Task<Solution> FixConfigurationMethodSignature(Document document, MethodDeclarationSyntax methodDecl, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            if (root == null)
            {
                return document.Project.Solution;
            }

            var existingBuilderParameter = methodDecl.ParameterList
                                                     .Parameters
                                                     .FirstOrDefault(p => p.Type is IdentifierNameSyntax n && n.Identifier.Text == Constants.CommandPipelineBuilderInterfaceName);

            var newMethodDeclaration = SyntaxFactory.MethodDeclaration(default,
                                                                       SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword)).Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword)),
                                                                       SyntaxFactory.ParseTypeName("void"),
                                                                       null,
                                                                       SyntaxFactory.Identifier(Constants.ConfigurePipelineMethodName),
                                                                       null,
                                                                       SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new List<ParameterSyntax>
                                                                       {
                                                                           SyntaxFactory.Parameter(default,
                                                                                                   default,
                                                                                                   SyntaxFactory.ParseTypeName(Constants.CommandPipelineBuilderInterfaceName),
                                                                                                   existingBuilderParameter?.Identifier ?? SyntaxFactory.Identifier("pipeline"),
                                                                                                   null),
                                                                       })),
                                                                       default,
                                                                       methodDecl.Body,
                                                                       methodDecl.ExpressionBody)
                                                    .WithSemicolonToken(methodDecl.SemicolonToken)
                                                    .NormalizeWhitespace()
                                                    .WithAdditionalAnnotations(Formatter.Annotation)
                                                    .WithLeadingTrivia(methodDecl.GetLeadingTrivia())
                                                    .WithTrailingTrivia(methodDecl.GetTrailingTrivia());

            return document.WithSyntaxRoot(root.ReplaceNode(methodDecl, newMethodDeclaration)).Project.Solution;
        }
    }
}
