using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Conqueror.CQS.Analyzers.Util;

public static class SymbolExtensions
{
    public static bool IsCommandHandlerType(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context)
    {
        return symbol?.Interfaces.Any(i => i.IsCommandHandlerInterfaceType(context)) ?? false;
    }

    public static bool IsCommandHandlerInterfaceType(this INamedTypeSymbol symbol, SyntaxNodeAnalysisContext context)
    {
        var commandHandlerWithoutResponseInterfaceType = context.Compilation.GetTypeByMetadataName("Conqueror.ICommandHandler`1");
        var commandHandlerInterfaceType = context.Compilation.GetTypeByMetadataName("Conqueror.ICommandHandler`2");

        if (commandHandlerInterfaceType == null || commandHandlerWithoutResponseInterfaceType == null)
        {
            return false;
        }

        if (symbol.IsEquivalent(commandHandlerInterfaceType) || symbol.IsEquivalent(commandHandlerWithoutResponseInterfaceType))
        {
            return true;
        }

        var declaredTypeSymbol = context.Compilation.GetTypeByMetadataName(symbol.ToString());

        return IsCommandHandlerType(declaredTypeSymbol, context);
    }

    public static bool HasConfigureCommandPipelineInterface(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context)
    {
        return symbol?.Interfaces.Any(i => i.IsConfigureCommandPipelineInterfaceType(context)) ?? false;
    }

    public static bool IsConfigureCommandPipelineInterfaceType(this INamedTypeSymbol symbol, SyntaxNodeAnalysisContext context)
    {
        var interfaceType = context.Compilation.GetTypeByMetadataName("Conqueror.IConfigureCommandPipeline");

        if (interfaceType == null)
        {
            return false;
        }

        return symbol.IsEquivalent(interfaceType);
    }

    public static bool IsQueryHandlerType(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context)
    {
        return symbol?.Interfaces.Any(i => i.IsQueryHandlerInterfaceType(context)) ?? false;
    }

    public static bool IsQueryHandlerInterfaceType(this INamedTypeSymbol symbol, SyntaxNodeAnalysisContext context)
    {
        var queryHandlerInterfaceType = context.Compilation.GetTypeByMetadataName("Conqueror.IQueryHandler`2");

        if (queryHandlerInterfaceType == null)
        {
            return false;
        }

        if (symbol.IsEquivalent(queryHandlerInterfaceType))
        {
            return true;
        }

        var declaredTypeSymbol = context.Compilation.GetTypeByMetadataName(symbol.ToString());

        return IsQueryHandlerType(declaredTypeSymbol, context);
    }

    public static bool HasConfigureQueryPipelineInterface(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context)
    {
        return symbol?.Interfaces.Any(i => i.IsConfigureQueryPipelineInterfaceType(context)) ?? false;
    }

    public static bool IsConfigureQueryPipelineInterfaceType(this INamedTypeSymbol symbol, SyntaxNodeAnalysisContext context)
    {
        var interfaceType = context.Compilation.GetTypeByMetadataName("Conqueror.IConfigureQueryPipeline");

        if (interfaceType == null)
        {
            return false;
        }

        return symbol.IsEquivalent(interfaceType);
    }

    public static bool IsEquivalent(this ISymbol symbol1, ISymbol symbol2)
    {
        return symbol1.MetadataName == symbol2.MetadataName && symbol1.ContainingAssembly.Name == symbol2.ContainingAssembly.Name;
    }
}
