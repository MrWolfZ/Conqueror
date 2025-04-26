using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Conqueror.SourceGenerators.Util;
using Microsoft.CodeAnalysis;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Messaging;

[Generator]
public sealed class MessageHandlerTypeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.InitializeGeneratorForHandlerTypes(GetMessageHandlerDescriptor, MessageHandlerTypeSources.GenerateMessageHandlerType);
    }

    private static MessageHandlerTypeDescriptor? GetMessageHandlerDescriptor(GeneratorSyntaxContext context, CancellationToken ct)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol handlerTypeSymbol)
        {
            // weird, we couldn't get the symbol, ignore it
            return null;
        }

        var messageTypeSymbols = handlerTypeSymbol.AllInterfaces
                                                  .Concat([handlerTypeSymbol.BaseType])
                                                  .OfType<INamedTypeSymbol>()
                                                  .Where(s => s.Name == "IHandler" && s.ContainingType is not null)
                                                  .Select(s => s.ContainingType)
                                                  .Where(s => s.IsMessageType())
                                                  .ToList();

        var diagnostics = new List<DiagnosticWithLocationDescriptor>();

        if (messageTypeSymbols.Count == 0)
        {
            return null;
        }

        ct.ThrowIfCancellationRequested();

        return GenerateHandlerDescriptor(handlerTypeSymbol, messageTypeSymbols, new([..diagnostics]), context.SemanticModel, ct);
    }

    private static MessageHandlerTypeDescriptor? GenerateHandlerDescriptor(INamedTypeSymbol handlerTypeSymbol,
                                                                           List<INamedTypeSymbol> messageTypeSymbols,
                                                                           EquatableArray<DiagnosticWithLocationDescriptor> diagnostics,
                                                                           SemanticModel semanticModel,
                                                                           CancellationToken ct)
    {
        var handlerTypeDescriptor = GeneratorHelper.GenerateTypeDescriptor(handlerTypeSymbol, semanticModel);
        var messageTypeDescriptors = messageTypeSymbols.Select(s => MessageTypeGenerator.GetMessageTypesDescriptor(s, semanticModel, ct))
                                                       .OfType<MessageTypeDescriptor>()
                                                       .ToArray();

        if (messageTypeDescriptors.Length == 0)
        {
            return null;
        }

        return new(handlerTypeDescriptor, new(messageTypeDescriptors), diagnostics);
    }
}
