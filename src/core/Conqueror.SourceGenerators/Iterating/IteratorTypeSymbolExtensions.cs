#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Iterating;

using Microsoft.CodeAnalysis;

public static class IteratorTypeSymbolExtensions
{
    public static bool IsIteratorType(this INamedTypeSymbol typeSymbol) =>
        typeSymbol.GetAttributes().Any(a => IsIteratorTransportAttribute(a.AttributeClass));

    public static bool IsIteratorTransportAttribute(this INamedTypeSymbol? attributeSymbol)
    {
        return attributeSymbol
            ?.GetAttributes()
            .Any(a =>
                string.Equals(
                    a.AttributeClass?.ToString(),
                    "Conqueror.Iterating.IteratorTransportAttribute",
                    StringComparison.Ordinal
                )
            ) ?? false;
    }

    public static (
        string Prefix,
        string Namespace,
        string? FullyQualifiedIteratorTypeName
        ) GetIteratorTransportAttributeProperties(this INamedTypeSymbol attributeSymbol)
    {
        var namedArguments = attributeSymbol
            .GetAttributes()
            .First(a =>
                string.Equals(
                    a.AttributeClass?.ToString(),
                    "Conqueror.Iterating.IteratorTransportAttribute",
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

        var iteratorTypeName =
            namedArguments
                .Where(a => string.Equals(a.Key, "FullyQualifiedIteratorTypeName", StringComparison.Ordinal))
                .Select(a => a.Value.Value)
                .FirstOrDefault() as string;

        return (prefix, ns, iteratorTypeName);
    }
}
