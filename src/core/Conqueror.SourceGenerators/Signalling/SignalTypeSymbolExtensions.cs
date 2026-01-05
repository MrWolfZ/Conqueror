#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Signalling;

using Microsoft.CodeAnalysis;

public static class SignalTypeSymbolExtensions
{
    public static bool IsSignalType(this INamedTypeSymbol typeSymbol) =>
        typeSymbol.GetAttributes().Any(a => IsSignalTransportAttribute(a.AttributeClass));

    public static bool IsSignalTransportAttribute(this INamedTypeSymbol? attributeSymbol)
    {
        return attributeSymbol
                ?.GetAttributes()
                .Any(a =>
                    string.Equals(
                        a.AttributeClass?.ToString(),
                        "Conqueror.Signalling.SignalTransportAttribute",
                        StringComparison.Ordinal
                    )
                ) ?? false;
    }

    public static (
        string Prefix,
        string Namespace,
        string? FullyQualifiedSignalTypeName
    ) GetPrefixAndNamespaceFromSignalTransportAttribute(this INamedTypeSymbol attributeSymbol)
    {
        var namedArguments = attributeSymbol
            .GetAttributes()
            .First(a =>
                string.Equals(
                    a.AttributeClass?.ToString(),
                    "Conqueror.Signalling.SignalTransportAttribute",
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

        var signalTypeName =
            namedArguments
                .Where(a => string.Equals(a.Key, "FullyQualifiedSignalTypeName", StringComparison.Ordinal))
                .Select(a => a.Value.Value)
                .FirstOrDefault() as string;

        return (prefix, ns, signalTypeName);
    }
}
