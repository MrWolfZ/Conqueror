using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Util.Signalling;

public static class SignallingAbstractionsGeneratorUtil
{
    public static void InitializeCoreSignallingAbstractionsGenerator(
        this IncrementalGeneratorInitializationContext context,
        string fullyQualifiedMetadataName)
    {
        context.InitializeGeneratorForAttribute(fullyQualifiedMetadataName,
                                                (ctx, ct) => GetSignalTypesDescriptor(ctx, fullyQualifiedMetadataName, ct),
                                                SignallingAbstractionsSources.GenerateSignalTypes);
    }

    public static SignalTypesDescriptor GenerateSignalTypesDescriptor(INamedTypeSymbol signalTypeSymbol,
                                                                      SemanticModel semanticModel)
    {
        var signalTypeDescriptor = GeneratorHelper.GenerateTypeDescriptor(signalTypeSymbol, semanticModel);

        var serializerContextTypeFromGlobalLookup = semanticModel.Compilation.GetTypeByMetadataName($"{signalTypeDescriptor.FullyQualifiedName}JsonSerializerContext");
        var serializerContextTypeFromSiblingLookup = signalTypeSymbol.ContainingType?.GetTypeMembers().FirstOrDefault(m => m.Name == $"{signalTypeDescriptor.Name}JsonSerializerContext");

        return new(signalTypeDescriptor,
                   serializerContextTypeFromGlobalLookup is not null || serializerContextTypeFromSiblingLookup is not null);
    }

    private static SignalTypesDescriptor? GetSignalTypesDescriptor(GeneratorAttributeSyntaxContext context,
                                                                   string fullyQualifiedMetadataName,
                                                                   CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol signalTypeSymbol)
        {
            // weird, we couldn't get the symbol, ignore it
            return null;
        }

        // skip signal types that already declare a types member
        // TODO: improve by simply skipping the generation of the property / nested type instead of ignoring the whole type
        if (signalTypeSymbol.MemberNames.Contains("T"))
        {
            return null;
        }

        // TODO: error if multiple attributes with conflicting response types are found

        // find all marker attributes on the type and select the one with the lowest alphabetical
        // order to be the one that should be processed
        var attributeClassToProcess = signalTypeSymbol.GetAttributes()
                                                      .Where(a => IsMarkerAttribute(a.AttributeClass))
                                                      .Select(a => a.AttributeClass)
                                                      .OfType<INamedTypeSymbol>()
                                                      .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                                                      .FirstOrDefault();

        static bool IsMarkerAttribute(INamedTypeSymbol? attributeSymbol)
        {
            while (attributeSymbol?.BaseType is { } baseType)
            {
                if (baseType.ToString() == "Conqueror.Signalling.ConquerorSignalTransportAttribute")
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

        return GenerateSignalTypesDescriptor(signalTypeSymbol, context.SemanticModel);
    }
}
