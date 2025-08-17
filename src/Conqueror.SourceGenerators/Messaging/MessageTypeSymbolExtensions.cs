#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Messaging;

using Microsoft.CodeAnalysis;

public static class MessageTypeSymbolExtensions
{
    public static bool IsMessageType(this INamedTypeSymbol typeSymbol) =>
        typeSymbol.GetAttributes().Any(a => IsMessageTransportAttribute(a.AttributeClass));

    public static bool IsMessageTransportAttribute(this INamedTypeSymbol? attributeSymbol)
    {
        return attributeSymbol
                ?.GetAttributes()
                .Any(a =>
                    string.Equals(
                        a.AttributeClass?.ToString(),
                        "Conqueror.Messaging.MessageTransportAttribute",
                        StringComparison.Ordinal
                    )
                ) ?? false;
    }

    public static (
        string Prefix,
        string Namespace,
        string? FullyQualifiedMessageTypeName
    ) GetMessageTransportAttributeProperties(this INamedTypeSymbol attributeSymbol)
    {
        var namedArguments = attributeSymbol
            .GetAttributes()
            .First(a =>
                string.Equals(
                    a.AttributeClass?.ToString(),
                    "Conqueror.Messaging.MessageTransportAttribute",
                    StringComparison.Ordinal
                )
            )
            .NamedArguments;

        var prefix =
            namedArguments.First(a => string.Equals(a.Key, "Prefix", StringComparison.Ordinal)).Value.Value as string
            ?? "";

        var ns =
            namedArguments.First(a => string.Equals(a.Key, "Namespace", StringComparison.Ordinal)).Value.Value as string
            ?? "";

        var messageTypeName =
            namedArguments
                .Where(a => string.Equals(a.Key, "FullyQualifiedMessageTypeName", StringComparison.Ordinal))
                .Select(a => a.Value.Value)
                .FirstOrDefault() as string;

        return (prefix, ns, messageTypeName);
    }
}
