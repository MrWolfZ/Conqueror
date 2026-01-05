#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Iterating;

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
public sealed class IteratorTypeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.InitializeGeneratorForAttribute(
            "Iterator",
            GetIteratorTypesDescriptor,
            IteratorTypeSources.GenerateIteratorTypeFile
        );
    }

    [SuppressMessage(
        "Design",
        "MA0045:Do not use blocking calls in a sync method (need to make calling method async)",
        Justification = "method is not async"
    )]
    internal static IteratorTypeDescriptor? GetIteratorTypesDescriptor(
        INamedTypeSymbol iteratorTypeSymbol,
        SemanticModel semanticModel,
        CancellationToken ct
    )
    {
        ITypeSymbol? itemTypeSymbol = null;

        var attribute = iteratorTypeSymbol
            .GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.IsIteratorTransportAttribute() ?? false)
            ?.AttributeClass;

        var diagnostics = new List<DiagnosticWithLocationDescriptor>();

        var itemTypes = iteratorTypeSymbol
            .GetAttributes()
            .Where(a => a.AttributeClass?.IsIteratorTransportAttribute() ?? false)
            .Select(a => a.AttributeClass?.TypeArguments.FirstOrDefault())
            .OfType<ITypeSymbol>()
            .Distinct(SymbolEqualityComparer.Default)
            .ToList();

        if (itemTypes.Count > 1)
        {
            var diag = new DiagnosticDescriptor(
                "CONQI0001",
                "Iterator type has multiple iterator attributes with inconsistent item types",
                "Iterator type has multiple iterator attributes with inconsistent item types",
                "Conqueror.Iterating",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true
            );

            var typeDeclarationSyntax =
                iteratorTypeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(ct) as TypeDeclarationSyntax;
            diagnostics.Add(new(diag, LocationDescriptor.CreateFrom(typeDeclarationSyntax?.Identifier)));
        }

        if (attribute is null)
        {
            return null;
        }

        if (attribute.TypeArguments.Length > 0)
        {
            itemTypeSymbol = attribute.TypeArguments[0];
        }

        if (itemTypeSymbol is null)
        {
            var diag = new DiagnosticDescriptor(
                "CONQI0002",
                "Iterator type must have an item type",
                "Iterator type must have an item type",
                "Conqueror.Iterating",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true
            );

            var typeDeclarationSyntax =
                iteratorTypeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(ct) as TypeDeclarationSyntax;
            diagnostics.Add(new(diag, LocationDescriptor.CreateFrom(typeDeclarationSyntax?.Identifier)));

            return null;
        }

        ct.ThrowIfCancellationRequested();

        return GetIteratorTypesDescriptor(iteratorTypeSymbol, itemTypeSymbol, semanticModel, new([.. diagnostics]));
    }

    private static IteratorTypeDescriptor? GetIteratorTypesDescriptor(
        GeneratorSyntaxContext context,
        CancellationToken ct
    )
    {
        if (context.SemanticModel.GetDeclaredSymbolSafe(context.Node) is not INamedTypeSymbol iteratorTypeSymbol)
        {
            return null;
        }

        if (
            string.Equals(iteratorTypeSymbol.ContainingAssembly?.Name, "Conqueror.Tests", StringComparison.Ordinal)
            && string.Equals(
                iteratorTypeSymbol.ContainingType?.Name,
                "IteratorTypeGenerationTests",
                StringComparison.Ordinal
            )
        )
        {
            return null;
        }

        ct.ThrowIfCancellationRequested();

        return GetIteratorTypesDescriptor(iteratorTypeSymbol, context.SemanticModel, ct);
    }

    private static IteratorTypeDescriptor GetIteratorTypesDescriptor(
        INamedTypeSymbol iteratorTypeSymbol,
        ITypeSymbol itemTypeSymbol,
        SemanticModel semanticModel,
        EquatableArray<DiagnosticWithLocationDescriptor> diagnostics
    )
    {
        var iteratorTypeDescriptor = GeneratorHelper.GenerateTypeDescriptor(iteratorTypeSymbol, semanticModel);
        var attributeDescriptors = iteratorTypeSymbol
            .GetAttributes()
            .Where(a => a.AttributeClass?.IsIteratorTransportAttribute() ?? false)
            .Select(a => GenerateIteratorAttributeDescriptor(a, a.AttributeClass!))
            .ToArray();

        var serializerContextTypeFromGlobalLookup = semanticModel.Compilation.GetTypeByMetadataName(
            $"{iteratorTypeDescriptor.FullyQualifiedName}JsonSerializerContext"
        );
        var serializerContextTypeFromSiblingLookup = iteratorTypeSymbol
            .ContainingType?.GetTypeMembers()
            .FirstOrDefault(m =>
                string.Equals(m.Name, $"{iteratorTypeDescriptor.Name}JsonSerializerContext", StringComparison.Ordinal)
            );

        return new IteratorTypeDescriptor(
            iteratorTypeDescriptor,
            GeneratorHelper.GenerateTypeDescriptor(itemTypeSymbol, semanticModel),
            new(attributeDescriptors),
            serializerContextTypeFromGlobalLookup is not null || serializerContextTypeFromSiblingLookup is not null,
            diagnostics
        );
    }

    private static IteratorAttributeDescriptor GenerateIteratorAttributeDescriptor(
        AttributeData attributeData,
        INamedTypeSymbol attributeSymbol
    )
    {
        var (prefix, ns, iteratorTypeName) = attributeSymbol.GetIteratorTransportAttributeProperties();

        return new IteratorAttributeDescriptor(
            prefix,
            ns,
            iteratorTypeName,
            GeneratorHelper.GetAttributeProperties(attributeData)
        );
    }
}
