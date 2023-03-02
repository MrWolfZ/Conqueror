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

namespace Conqueror.CQS.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzerCodeFixProvider))]
[Shared]
public sealed class C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzerCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(C0001CommandHandlerWithPipelineConfigurationInterfaceHasConfigurationMethodAnalyzer.DiagnosticId);

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

        var notImplementedExceptionType = SyntaxFactory.ParseTypeName("System.NotImplementedException");

        var methodDeclaration = SyntaxFactory.MethodDeclaration(default,
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
                                                                                            SyntaxFactory.Identifier("pipeline"),
                                                                                            null),
                                                                })),
                                                                default,
                                                                null,
                                                                SyntaxFactory.ArrowExpressionClause(
                                                                    SyntaxFactory.ThrowExpression(SyntaxFactory.ObjectCreationExpression(notImplementedExceptionType)
                                                                                                               .WithArgumentList(SyntaxFactory.ArgumentList()))))
                                             .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                                             .NormalizeWhitespace()
                                             .WithAdditionalAnnotations(Formatter.Annotation);

        var newLineText = document.Project.Solution.Workspace.Options.GetOption(FormattingOptions.NewLine, LanguageNames.CSharp);

        var indexOfFirstMethod = typeDecl.Members.Select((m, i) => new { Member = m, Index = i }).FirstOrDefault(a => a.Member is MethodDeclarationSyntax)?.Index ?? typeDecl.Members.Count;

        if (indexOfFirstMethod > 0)
        {
            methodDeclaration = methodDeclaration.WithLeadingTrivia(SyntaxFactory.EndOfLine(newLineText));
        }

        if (indexOfFirstMethod < typeDecl.Members.Count)
        {
            methodDeclaration = methodDeclaration.WithTrailingTrivia(SyntaxFactory.EndOfLine(newLineText));
        }

        var newMembers = typeDecl.Members.Insert(indexOfFirstMethod, methodDeclaration);

        return document.WithSyntaxRoot(
            root.ReplaceNode(
                typeDecl,
                typeDecl.WithMembers(newMembers))).Project.Solution;
    }
}
