using System.Linq;
using Microsoft.CodeAnalysis;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Signalling;

public static class SignalTypeSymbolExtensions
{
    public static bool IsSignalType(this INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.GetAttributes().Any(a => IsSignalTransportAttribute(a.AttributeClass));
    }

    public static bool IsSignalTransportAttribute(this INamedTypeSymbol? attributeSymbol)
    {
        return attributeSymbol?.GetAttributes().Any(a => a.AttributeClass?.ToString() == "Conqueror.Signalling.SignalTransportAttribute") ?? false;
    }

    public static (string Prefix, string Namespace) GetPrefixAndNamespaceFromSignalTransportAttribute(this INamedTypeSymbol attributeSymbol)
    {
        var namedArguments = attributeSymbol.GetAttributes()
                                            .First(a => a.AttributeClass?.ToString() == "Conqueror.Signalling.SignalTransportAttribute")
                                            .NamedArguments;

        var prefix = namedArguments.First(a => a.Key == "Prefix").Value.Value as string ?? string.Empty;
        var ns = namedArguments.First(a => a.Key == "Namespace").Value.Value as string ?? string.Empty;

        return (prefix, ns);
    }
}
