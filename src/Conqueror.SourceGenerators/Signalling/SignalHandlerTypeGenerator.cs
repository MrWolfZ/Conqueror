using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Conqueror.SourceGenerators.Util;
using Microsoft.CodeAnalysis;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Signalling;

[Generator]
public sealed class SignalHandlerTypeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.InitializeGeneratorForHandlerTypes(GetSignalHandlerDescriptor, SignalHandlerTypeSources.GenerateSignalHandlerType);
    }

    private static SignalHandlerTypeDescriptor? GetSignalHandlerDescriptor(GeneratorSyntaxContext context, CancellationToken ct)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol handlerTypeSymbol)
        {
            // weird, we couldn't get the symbol, ignore it
            return null;
        }

        var signalTypeSymbols = handlerTypeSymbol.AllInterfaces
                                                 .Concat([handlerTypeSymbol.BaseType])
                                                 .OfType<INamedTypeSymbol>()
                                                 .Where(s => s.Name == "IHandler" && s.ContainingType is not null)
                                                 .Select(s => s.ContainingType)
                                                 .Where(s => s.IsSignalType())
                                                 .ToList();

        if (signalTypeSymbols.Count == 0)
        {
            return null;
        }

        ct.ThrowIfCancellationRequested();

        return GenerateHandlerDescriptor(handlerTypeSymbol, signalTypeSymbols, context.SemanticModel);
    }

    private static SignalHandlerTypeDescriptor GenerateHandlerDescriptor(INamedTypeSymbol handlerTypeSymbol,
                                                                         List<INamedTypeSymbol> signalTypeSymbols,
                                                                         SemanticModel semanticModel)
    {
        var handlerTypeDescriptor = GeneratorHelper.GenerateTypeDescriptor(handlerTypeSymbol, semanticModel);
        var signalTypeDescriptors = signalTypeSymbols.Select(s => SignalTypeGenerator.GetSignalTypesDescriptor(s, semanticModel)).ToArray();

        return new(handlerTypeDescriptor, new(signalTypeDescriptors));
    }
}
