#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Messaging;

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
public sealed class MessageTypeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.InitializeGeneratorForAttribute(
            "Message",
            GetMessageTypesDescriptor,
            MessageTypeSources.GenerateMessageTypeFile
        );
    }

    [SuppressMessage(
        "Design",
        "MA0045:Do not use blocking calls in a sync method (need to make calling method async)",
        Justification = "method is not async"
    )]
    internal static MessageTypeDescriptor? GetMessageTypesDescriptor(
        INamedTypeSymbol messageTypeSymbol,
        SemanticModel semanticModel,
        CancellationToken ct
    )
    {
        ITypeSymbol? responseTypeSymbol = null;

        var attribute = messageTypeSymbol
            .GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.IsMessageTransportAttribute() ?? false)
            ?.AttributeClass;

        var diagnostics = new List<DiagnosticWithLocationDescriptor>();

        var responseTypes = messageTypeSymbol
            .GetAttributes()
            .Where(a => a.AttributeClass?.IsMessageTransportAttribute() ?? false)
            .Select(a => a.AttributeClass?.TypeArguments.FirstOrDefault())
            .OfType<ITypeSymbol>()
            .Distinct(SymbolEqualityComparer.Default)
            .ToList();

        if (responseTypes.Count > 1)
        {
            var diag = new DiagnosticDescriptor(
                "CONQM0001",
                "Message type has multiple message attributes with inconsistent response types",
                "Message type has multiple message attributes with inconsistent response types",
                "Conqueror.Messaging",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true
            );

            var typeDeclarationSyntax =
                messageTypeSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(ct) as TypeDeclarationSyntax;
            diagnostics.Add(new(diag, LocationDescriptor.CreateFrom(typeDeclarationSyntax?.Identifier)));
        }

        // we did not find a message attribute (e.g. a false positive from "[SuppressMessage(...)]")
        if (attribute is null)
        {
            return null;
        }

        if (attribute.TypeArguments.Length > 0)
        {
            responseTypeSymbol = attribute.TypeArguments[0];
        }

        ct.ThrowIfCancellationRequested();

        return GetMessageTypesDescriptor(messageTypeSymbol, responseTypeSymbol, semanticModel, new([.. diagnostics]));
    }

    private static MessageTypeDescriptor? GetMessageTypesDescriptor(
        GeneratorSyntaxContext context,
        CancellationToken ct
    )
    {
        if (context.SemanticModel.GetDeclaredSymbolSafe(context.Node) is not INamedTypeSymbol messageTypeSymbol)
        {
            // weird, we couldn't get the symbol, ignore it
            return null;
        }

        // skip message types in our special test class
        // TODO: improve the generator by lazily generating all properties that have not been defined yet
        if (
            string.Equals(messageTypeSymbol.ContainingAssembly?.Name, "Conqueror.Tests", StringComparison.Ordinal)
            && string.Equals(
                messageTypeSymbol.ContainingType?.Name,
                "MessageTypeGenerationTests",
                StringComparison.Ordinal
            )
        )
        {
            return null;
        }

        ct.ThrowIfCancellationRequested();

        return GetMessageTypesDescriptor(messageTypeSymbol, context.SemanticModel, ct);
    }

    private static MessageTypeDescriptor GetMessageTypesDescriptor(
        INamedTypeSymbol messageTypeSymbol,
        ITypeSymbol? responseTypeSymbol,
        SemanticModel semanticModel,
        EquatableArray<DiagnosticWithLocationDescriptor> diagnostics
    )
    {
        var messageTypeDescriptor = GeneratorHelper.GenerateTypeDescriptor(messageTypeSymbol, semanticModel);
        var attributeDescriptors = messageTypeSymbol
            .GetAttributes()
            .Where(a => a.AttributeClass?.IsMessageTransportAttribute() ?? false)
            .Select(a => GenerateMessageAttributeDescriptor(a, a.AttributeClass!))
            .ToArray();

        var serializerContextTypeFromGlobalLookup = semanticModel.Compilation.GetTypeByMetadataName(
            $"{messageTypeDescriptor.FullyQualifiedName}JsonSerializerContext"
        );
        var serializerContextTypeFromSiblingLookup = messageTypeSymbol
            .ContainingType?.GetTypeMembers()
            .FirstOrDefault(m =>
                string.Equals(m.Name, $"{messageTypeDescriptor.Name}JsonSerializerContext", StringComparison.Ordinal)
            );

        return new MessageTypeDescriptor(
            messageTypeDescriptor,
            responseTypeSymbol is not null
                ? GeneratorHelper.GenerateTypeDescriptor(responseTypeSymbol, semanticModel)
                : GenerateUnitResponseTypeDescriptor(),
            new(attributeDescriptors),
            serializerContextTypeFromGlobalLookup is not null || serializerContextTypeFromSiblingLookup is not null,
            diagnostics
        );
    }

    private static MessageAttributeDescriptor GenerateMessageAttributeDescriptor(
        AttributeData attributeData,
        INamedTypeSymbol attributeSymbol
    )
    {
        var (prefix, ns, messageTypeName) = attributeSymbol.GetMessageTransportAttributeProperties();

        return new MessageAttributeDescriptor(
            prefix,
            ns,
            messageTypeName,
            GeneratorHelper.GetAttributeProperties(attributeData)
        );
    }

    private static TypeDescriptor GenerateUnitResponseTypeDescriptor()
    {
        return new(
            "UnitMessageResponse",
            "UnitMessageResponse",
            "Conqueror",
            "Conqueror.UnitMessageResponse",
            Accessibility.Public,
            IsRecord: true,
            IsAbstract: false,
            IsPrimitive: false,
            TypeArguments: default,
            TypeConstraints: null,
            Attributes: default,
            Properties: default,
            Methods: default,
            BaseTypes: default,
            Interfaces: default,
            ParentClasses: default,
            Enumerable: null,
            Tuple: null
        );
    }
}
