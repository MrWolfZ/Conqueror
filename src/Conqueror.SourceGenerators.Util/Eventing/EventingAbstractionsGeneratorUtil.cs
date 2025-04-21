using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Util.Eventing;

public static class EventingAbstractionsGeneratorUtil
{
    public static void InitializeCoreEventingAbstractionsGenerator(
        this IncrementalGeneratorInitializationContext context,
        string fullyQualifiedMetadataName)
    {
        context.InitializeGeneratorForAttribute(fullyQualifiedMetadataName,
                                                (ctx, ct) => GetEventNotificationTypesDescriptor(ctx, fullyQualifiedMetadataName, ct),
                                                EventingAbstractionsSources.GenerateEventNotificationTypes);
    }

    public static EventNotificationTypesDescriptor GenerateEventNotificationTypesDescriptor(INamedTypeSymbol eventNotificationTypeSymbol,
                                                                                            SemanticModel semanticModel)
    {
        var eventNotificationTypeDescriptor = GeneratorHelper.GenerateTypeDescriptor(eventNotificationTypeSymbol, semanticModel);

        var serializerContextTypeFromGlobalLookup = semanticModel.Compilation.GetTypeByMetadataName($"{eventNotificationTypeDescriptor.FullyQualifiedName}JsonSerializerContext");
        var serializerContextTypeFromSiblingLookup = eventNotificationTypeSymbol.ContainingType?.GetTypeMembers().FirstOrDefault(m => m.Name == $"{eventNotificationTypeDescriptor.Name}JsonSerializerContext");

        return new(eventNotificationTypeDescriptor,
                   serializerContextTypeFromGlobalLookup is not null || serializerContextTypeFromSiblingLookup is not null);
    }

    private static EventNotificationTypesDescriptor? GetEventNotificationTypesDescriptor(GeneratorAttributeSyntaxContext context,
                                                                                         string fullyQualifiedMetadataName,
                                                                                         CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol eventNotificationTypeSymbol)
        {
            // weird, we couldn't get the symbol, ignore it
            return null;
        }

        // skip event notification types that already declare a types member
        // TODO: improve by simply skipping the generation of the property / nested type instead of ignoring the whole type
        if (eventNotificationTypeSymbol.MemberNames.Contains("T"))
        {
            return null;
        }

        // TODO: error if multiple attributes with conflicting response types are found

        // find all marker attributes on the type and select the one with the lowest alphabetical
        // order to be the one that should be processed
        var attributeClassToProcess = eventNotificationTypeSymbol.GetAttributes()
                                                       .Where(a => IsMarkerAttribute(a.AttributeClass))
                                                       .Select(a => a.AttributeClass)
                                                       .OfType<INamedTypeSymbol>()
                                                       .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                                                       .FirstOrDefault();

        static bool IsMarkerAttribute(INamedTypeSymbol? attributeSymbol)
        {
            while (attributeSymbol?.BaseType is { } baseType)
            {
                if (baseType.ToString() == "Conqueror.Eventing.ConquerorEventNotificationTransportAttribute")
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

        ct.ThrowIfCancellationRequested();

        return GenerateEventNotificationTypesDescriptor(eventNotificationTypeSymbol, context.SemanticModel);
    }
}
