using System.Linq;
using System.Threading;
using Conqueror.SourceGenerators.Util;
using Microsoft.CodeAnalysis;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Signalling;

[Generator]
public sealed class SignalTypeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.InitializeGeneratorForAttribute("Signal", GetSignalTypesDescriptor, SignalTypeSources.GenerateSignalTypeFile);
    }

    public static SignalTypeDescriptor? GetSignalTypesDescriptor(INamedTypeSymbol signalTypeSymbol, SemanticModel semanticModel)
    {
        var attribute = signalTypeSymbol.GetAttributes()
                                        .FirstOrDefault(a => a.AttributeClass?.IsSignalTransportAttribute() ?? false)
                                        ?.AttributeClass;

        // we did not find a message attribute (e.g. a false positive from "[SomeOtherSignal(...)]")
        if (attribute is null)
        {
            return null;
        }

        var signalTypeDescriptor = GeneratorHelper.GenerateTypeDescriptor(signalTypeSymbol, semanticModel);
        var attributeDescriptors = signalTypeSymbol.GetAttributes()
                                                   .Where(a => a.AttributeClass?.IsSignalTransportAttribute() ?? false)
                                                   .Select(a => GenerateSignalAttributeDescriptor(a, a.AttributeClass!))
                                                   .ToArray();

        var serializerContextTypeFromGlobalLookup = semanticModel.Compilation.GetTypeByMetadataName($"{signalTypeDescriptor.FullyQualifiedName}JsonSerializerContext");
        var serializerContextTypeFromSiblingLookup = signalTypeSymbol.ContainingType?.GetTypeMembers().FirstOrDefault(m => m.Name == $"{signalTypeDescriptor.Name}JsonSerializerContext");

        return new(signalTypeDescriptor,
                   new(attributeDescriptors),
                   serializerContextTypeFromGlobalLookup is not null || serializerContextTypeFromSiblingLookup is not null);
    }

    private static SignalTypeDescriptor? GetSignalTypesDescriptor(GeneratorSyntaxContext context, CancellationToken ct)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol signalTypeSymbol)
        {
            // weird, we couldn't get the symbol, ignore it
            return null;
        }

        // skip signal types in our special test class
        // TODO: improve the generator by lazily generating all properties that have not been defined yet
        if (signalTypeSymbol.ContainingAssembly?.Name == "Conqueror.Tests" && signalTypeSymbol.ContainingType?.Name == "SignalTypeGenerationTests")
        {
            return null;
        }

        ct.ThrowIfCancellationRequested();

        return GetSignalTypesDescriptor(signalTypeSymbol, context.SemanticModel);
    }

    private static SignalAttributeDescriptor GenerateSignalAttributeDescriptor(AttributeData attributeData, INamedTypeSymbol attributeSymbol)
    {
        var (prefix, ns, signalTypeName) = attributeSymbol.GetPrefixAndNamespaceFromSignalTransportAttribute();
        return new(prefix, ns, signalTypeName, GeneratorHelper.GetAttributeProperties(attributeData));
    }
}
