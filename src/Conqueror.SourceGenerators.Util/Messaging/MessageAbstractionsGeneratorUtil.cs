using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Util.Messaging;

public static class MessageAbstractionsGeneratorUtil
{
    public static void InitializeCoreMessageAbstractionsGenerator(
        this IncrementalGeneratorInitializationContext context,
        string fullyQualifiedMetadataName)
    {
        context.InitializeGeneratorForAttribute(fullyQualifiedMetadataName,
                                                (ctx, ct) => GetMessageTypesDescriptor(ctx, fullyQualifiedMetadataName, ct),
                                                MessageAbstractionsGenerationHelper.GenerateMessageTypes);

        if (!fullyQualifiedMetadataName.EndsWith("`1"))
        {
            // register generic overload with response type
            context.InitializeCoreMessageAbstractionsGenerator(fullyQualifiedMetadataName + "`1");
        }
    }

    private static MessageTypesDescriptor? GetMessageTypesDescriptor(GeneratorAttributeSyntaxContext context,
                                                                     string fullyQualifiedMetadataName,
                                                                     CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol messageTypeSymbol)
        {
            // weird, we couldn't get the symbol, ignore it
            return null;
        }

        // skip message types that already declare a handler member
        // TODO: improve by simply skipping the generation of the property / nested type instead of ignoring the whole type
        if (messageTypeSymbol.MemberNames.Contains("IHandler"))
        {
            return null;
        }

        // TODO: error if multiple attributes with conflicting response types are found

        // find all marker attributes on the type and select the one with the lowest alphabetical
        // order to be the one that should be processed
        var attributeClassToProcess = messageTypeSymbol.GetAttributes()
                                                       .Where(a => IsMarkerAttribute(a.AttributeClass))
                                                       .Select(a => a.AttributeClass)
                                                       .OfType<INamedTypeSymbol>()
                                                       .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                                                       .FirstOrDefault();

        static bool IsMarkerAttribute(INamedTypeSymbol? attributeSymbol)
        {
            while (attributeSymbol?.BaseType is { } baseType)
            {
                if (baseType.ToString() == "Conqueror.Messaging.ConquerorMessageTransportAttribute")
                {
                    return true;
                }

                attributeSymbol = baseType;
            }

            return false;
        }

        // only process the attribute if it is the one with the lowest alphabetical order
        var attributeClassToProcessFullyQualifiedMetadataName = $"{attributeClassToProcess?.ContainingNamespace}.{attributeClassToProcess?.MetadataName}";
        if (fullyQualifiedMetadataName != attributeClassToProcessFullyQualifiedMetadataName)
        {
            return null;
        }

        ITypeSymbol? responseTypeSymbol = null;

        if (attributeClassToProcess?.TypeArguments.Length > 0)
        {
            responseTypeSymbol = attributeClassToProcess.TypeArguments[0];
        }

        ct.ThrowIfCancellationRequested();

        return MessageAbstractionsGeneratorHelper.GenerateMessageTypesDescriptor(messageTypeSymbol, responseTypeSymbol);
    }
}
