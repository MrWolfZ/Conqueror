#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Messaging;

using Microsoft.CodeAnalysis;

[Generator]
public sealed class MessageHandlerTypeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.InitializeGeneratorForHandlerTypes(
            GetMessageHandlerDescriptor,
            MessageHandlerTypeSources.GenerateMessageHandlerType
        );
    }

    private static MessageHandlerTypeDescriptor? GetMessageHandlerDescriptor(
        GeneratorSyntaxContext context,
        CancellationToken ct
    )
    {
        if (context.SemanticModel.GetDeclaredSymbolSafe(context.Node) is not INamedTypeSymbol handlerTypeSymbol)
        {
            // weird, we couldn't get the symbol, ignore it
            return null;
        }

        var messageTypeSymbols = handlerTypeSymbol
            .AllInterfaces.Concat([handlerTypeSymbol.BaseType])
            .OfType<INamedTypeSymbol>()
            .Where(s => string.Equals(s.Name, "IHandler", StringComparison.Ordinal) && s.ContainingType is not null)
            .Select(s => s.ContainingType)
            .Where(s => s.IsMessageType())
            .ToList();

        if (messageTypeSymbols.Count is 0)
        {
            return null;
        }

        ct.ThrowIfCancellationRequested();

        return GenerateHandlerDescriptor(handlerTypeSymbol, messageTypeSymbols, context.SemanticModel, ct);
    }

    private static MessageHandlerTypeDescriptor? GenerateHandlerDescriptor(
        INamedTypeSymbol handlerTypeSymbol,
        List<INamedTypeSymbol> messageTypeSymbols,
        SemanticModel semanticModel,
        CancellationToken ct
    )
    {
        var handlerTypeDescriptor = GeneratorHelper.GenerateTypeDescriptor(handlerTypeSymbol, semanticModel);
        var messageTypeDescriptors = messageTypeSymbols
            .Select(s => MessageTypeGenerator.GetMessageTypesDescriptor(s, semanticModel, ct))
            .OfType<MessageTypeDescriptor>()
            .ToArray();

        if (messageTypeDescriptors.Length is 0)
        {
            return null;
        }

        return new MessageHandlerTypeDescriptor(handlerTypeDescriptor, new(messageTypeDescriptors), new([]));
    }
}
