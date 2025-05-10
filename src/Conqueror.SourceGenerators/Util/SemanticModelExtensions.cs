using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Conqueror.SourceGenerators.Util;

internal static class SemanticModelExtensions
{
    public static ISymbol? GetDeclaredSymbolSafe(this SemanticModel semanticModel, SyntaxNode node)
    {
        return semanticModel.GetSemanticModel(node).GetDeclaredSymbol(node);
    }

    public static INamedTypeSymbol? GetDeclaredSymbolSafe(this SemanticModel semanticModel, TypeDeclarationSyntax node)
    {
        return semanticModel.GetSemanticModel(node).GetDeclaredSymbol(node);
    }

    private static SemanticModel GetSemanticModel(this SemanticModel semanticModel, SyntaxNode node)
    {
        // during writing of tests for the Conqueror libraries, we ran into a rare issue
        // where the syntax tree of the symbol was different from the syntax tree of the
        // semantic model; we were unable to reproduce this with a unit test, but we found
        // this workaround to fix the issue
        if (node.SyntaxTree != semanticModel.SyntaxTree)
        {
            return semanticModel.Compilation.GetSemanticModel(node.SyntaxTree);
        }

        return semanticModel;
    }
}
