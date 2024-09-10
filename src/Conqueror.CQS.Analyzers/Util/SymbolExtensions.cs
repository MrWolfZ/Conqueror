using System.Linq;
using Microsoft.CodeAnalysis;

namespace Conqueror.CQS.Analyzers.Util;

public static class SymbolExtensions
{
    public static bool IsCommandHandlerType(this ITypeSymbol symbol, Compilation compilation)
    {
        return symbol?.Interfaces.Any(i => i.IsCommandHandlerInterfaceType(compilation)) ?? false;
    }

    public static bool IsCommandHandlerInterfaceType(this INamedTypeSymbol symbol, Compilation compilation)
    {
        var commandHandlerWithoutResponseInterfaceType = compilation.GetTypeByMetadataName("Conqueror.ICommandHandler`1");
        var commandHandlerInterfaceType = compilation.GetTypeByMetadataName("Conqueror.ICommandHandler`2");

        if (commandHandlerInterfaceType == null || commandHandlerWithoutResponseInterfaceType == null)
        {
            return false;
        }

        if (symbol.IsEquivalent(commandHandlerInterfaceType) || symbol.IsEquivalent(commandHandlerWithoutResponseInterfaceType))
        {
            return true;
        }

        var declaredTypeSymbol = compilation.GetTypeByMetadataName(symbol.ToString());

        return IsCommandHandlerType(declaredTypeSymbol, compilation);
    }

    public static bool IsQueryHandlerType(this ITypeSymbol symbol, Compilation compilation)
    {
        return symbol?.Interfaces.Any(i => i.IsQueryHandlerInterfaceType(compilation)) ?? false;
    }

    public static bool IsQueryHandlerInterfaceType(this INamedTypeSymbol symbol, Compilation compilation)
    {
        var queryHandlerInterfaceType = compilation.GetTypeByMetadataName("Conqueror.IQueryHandler`2");

        if (queryHandlerInterfaceType == null)
        {
            return false;
        }

        if (symbol.IsEquivalent(queryHandlerInterfaceType))
        {
            return true;
        }

        var declaredTypeSymbol = compilation.GetTypeByMetadataName(symbol.ToString());

        return IsQueryHandlerType(declaredTypeSymbol, compilation);
    }

    public static bool IsEquivalent(this ISymbol symbol1, ISymbol symbol2)
    {
        return symbol1.MetadataName == symbol2.MetadataName && symbol1.ContainingAssembly.Name == symbol2.ContainingAssembly.Name;
    }
}
