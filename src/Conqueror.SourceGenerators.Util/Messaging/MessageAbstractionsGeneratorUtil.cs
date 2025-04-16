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
        // TODO: remove this once the proper deduplication logic exists
        if (!fullyQualifiedMetadataName.StartsWith("Conqueror.MessageAttribute"))
        {
            return null;
        }

        if (context.TargetSymbol is not INamedTypeSymbol messageTypeSymbol)
        {
            // weird, we couldn't get the symbol, ignore it
            return null;
        }

        // skip message types that already declare a handler member
        // TODO: improve with adding explicit diagnostic and also detecting pipeline
        if (messageTypeSymbol.MemberNames.Contains("IHandler"))
        {
            return null;
        }

        INamedTypeSymbol? responseTypeSymbol = null;

        foreach (var attributeData in messageTypeSymbol.GetAttributes())
        {
            // TODO: find all attributes that inherit from the common base attribute and only
            // process this when the lowest present ID alphabetically is equal to the ID that
            // was passed in

            // TODO: allow for transport attributes as well
            // TODO: error if multiple attributes with conflicting response types are found
            if (attributeData.AttributeClass is { Name: "MessageAttribute" } c
                && c.ContainingAssembly.Name == "Conqueror.Abstractions")
            {
                if (c.TypeArguments.Length > 0)
                {
                    responseTypeSymbol = c.TypeArguments[0] as INamedTypeSymbol;
                }

                continue;
            }

            if (attributeData.AttributeClass is { Name: "HttpMessageAttribute" } c2
                && c2.ContainingAssembly.Name == "Conqueror.Transport.Http.Abstractions")
            {
                continue;
            }

            // if no attribute matches, we skip this type
            return null;
        }

        ct.ThrowIfCancellationRequested();

        return MessageAbstractionsGeneratorHelper.GenerateMessageTypeToGenerate(messageTypeSymbol, responseTypeSymbol);
    }
}
