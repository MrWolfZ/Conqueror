using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Util.Messaging;

public static class MessagingAbstractionsGeneratorUtil
{
    public static void InitializeCoreMessageAbstractionsGenerator(
        this IncrementalGeneratorInitializationContext context,
        string simpleAttributeNameSuffix,
        string fullyQualifiedMetadataName)
    {
        context.InitializeGeneratorForAttribute(simpleAttributeNameSuffix,
                                                (ctx, ct) => GetMessageTypesDescriptor(ctx, fullyQualifiedMetadataName, ct),
                                                MessagingAbstractionsSources.GenerateMessageTypes);
    }

    public static MessageTypesDescriptor GenerateMessageTypesDescriptor(INamedTypeSymbol messageTypeSymbol,
                                                                        ITypeSymbol? responseTypeSymbol,
                                                                        SemanticModel semanticModel)
    {
        var messageTypeDescriptor = GeneratorHelper.GenerateTypeDescriptor(messageTypeSymbol, semanticModel);

        var serializerContextTypeFromGlobalLookup = semanticModel.Compilation.GetTypeByMetadataName($"{messageTypeDescriptor.FullyQualifiedName}JsonSerializerContext");
        var serializerContextTypeFromSiblingLookup = messageTypeSymbol.ContainingType?.GetTypeMembers().FirstOrDefault(m => m.Name == $"{messageTypeDescriptor.Name}JsonSerializerContext");

        return new(messageTypeDescriptor,
                   responseTypeSymbol is not null ? GeneratorHelper.GenerateTypeDescriptor(responseTypeSymbol, semanticModel) : GenerateUnitResponseTypeDescriptor(),
                   serializerContextTypeFromGlobalLookup is not null || serializerContextTypeFromSiblingLookup is not null);
    }

    private static MessageTypesDescriptor? GetMessageTypesDescriptor(GeneratorSyntaxContext context,
                                                                     string fullyQualifiedMetadataName,
                                                                     CancellationToken ct)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol messageTypeSymbol)
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

        return GenerateMessageTypesDescriptor(messageTypeSymbol, responseTypeSymbol, context.SemanticModel);
    }

    private static TypeDescriptor GenerateUnitResponseTypeDescriptor()
    {
        return new(
            "UnitMessageResponse",
            "UnitMessageResponse",
            "Conqueror",
            "Conqueror.UnitMessageResponse",
            Accessibility.Public,
            true,
            TypeArguments: default,
            TypeConstraints: null,
            Attributes: default,
            BaseTypes: default,
            Interfaces: default,
            ParentClasses: default,
            Properties: default,
            Methods: default,
            Enumerable: null);
    }
}
