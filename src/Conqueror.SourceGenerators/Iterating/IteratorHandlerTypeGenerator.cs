#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Iterating;

using Microsoft.CodeAnalysis;

[Generator]
public sealed class IteratorHandlerTypeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.InitializeGeneratorForHandlerTypes(
            GetIteratorHandlerDescriptor,
            IteratorHandlerTypeSources.GenerateIteratorHandlerType
        );
    }

    private static IteratorHandlerTypeDescriptor? GetIteratorHandlerDescriptor(
        GeneratorSyntaxContext context,
        CancellationToken ct
    )
    {
        if (context.SemanticModel.GetDeclaredSymbolSafe(context.Node) is not INamedTypeSymbol handlerTypeSymbol)
        {
            return null;
        }

        var iteratorTypeSymbols = handlerTypeSymbol
            .AllInterfaces.Concat([handlerTypeSymbol.BaseType])
            .OfType<INamedTypeSymbol>()
            .Where(s => string.Equals(s.Name, "IHandler", StringComparison.Ordinal) && s.ContainingType is not null)
            .Select(s => s.ContainingType)
            .Where(s => s.IsIteratorType())
            .ToList();

        if (iteratorTypeSymbols.Count is 0)
        {
            return null;
        }

        ct.ThrowIfCancellationRequested();

        return GenerateHandlerDescriptor(handlerTypeSymbol, iteratorTypeSymbols, context.SemanticModel, ct);
    }

    private static IteratorHandlerTypeDescriptor? GenerateHandlerDescriptor(
        INamedTypeSymbol handlerTypeSymbol,
        List<INamedTypeSymbol> iteratorTypeSymbols,
        SemanticModel semanticModel,
        CancellationToken ct
    )
    {
        var handlerTypeDescriptor = GeneratorHelper.GenerateTypeDescriptor(handlerTypeSymbol, semanticModel);
        var iteratorTypeDescriptors = iteratorTypeSymbols
            .Select(s => IteratorTypeGenerator.GetIteratorTypesDescriptor(s, semanticModel, ct))
            .OfType<IteratorTypeDescriptor>()
            .ToArray();

        if (iteratorTypeDescriptors.Length is 0)
        {
            return null;
        }

        return new IteratorHandlerTypeDescriptor(handlerTypeDescriptor, new(iteratorTypeDescriptors), new([]));
    }
}
