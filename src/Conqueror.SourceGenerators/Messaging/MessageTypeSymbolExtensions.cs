using System.Linq;
using Microsoft.CodeAnalysis;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Messaging;

public static class MessageTypeSymbolExtensions
{
    public static bool IsMessageType(this INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.GetAttributes().Any(a => IsMessageTransportAttribute(a.AttributeClass));
    }

    public static bool IsMessageTransportAttribute(this INamedTypeSymbol? attributeSymbol)
    {
        return attributeSymbol?.GetAttributes().Any(a => a.AttributeClass?.ToString() == "Conqueror.Messaging.MessageTransportAttribute") ?? false;
    }

    public static (string Prefix, string Namespace) GetPrefixAndNamespaceFromMessageTransportAttribute(this INamedTypeSymbol attributeSymbol)
    {
        var namedArguments = attributeSymbol.GetAttributes()
                                            .First(a => a.AttributeClass?.ToString() == "Conqueror.Messaging.MessageTransportAttribute")
                                            .NamedArguments;

        var prefix = namedArguments.First(a => a.Key == "Prefix").Value.Value as string ?? string.Empty;
        var ns = namedArguments.First(a => a.Key == "Namespace").Value.Value as string ?? string.Empty;

        return (prefix, ns);
    }
}
