using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Conqueror.SourceGenerators.Util.Messaging;

public static class MessageAbstractionsGeneratorHelper
{
    public static MessageTypesDescriptor GenerateMessageTypesDescriptor(INamedTypeSymbol messageTypeSymbol,
                                                                        ITypeSymbol? responseTypeSymbol,
                                                                        SemanticModel semanticModel)
    {
        var messageTypeDescriptor = GenerateTypeDescriptor(messageTypeSymbol, semanticModel);

        var serializerContextTypeFromGlobalLookup = semanticModel.Compilation.GetTypeByMetadataName($"{messageTypeDescriptor.FullyQualifiedName}JsonSerializerContext");
        var serializerContextTypeFromSiblingLookup = messageTypeSymbol.ContainingType?.GetTypeMembers().FirstOrDefault(m => m.Name == $"{messageTypeDescriptor.Name}JsonSerializerContext");

        return new(messageTypeDescriptor,
                   responseTypeSymbol is not null ? GenerateTypeDescriptor(responseTypeSymbol, semanticModel) : GenerateUnitResponseTypeDescriptor(),
                   serializerContextTypeFromGlobalLookup is not null || serializerContextTypeFromSiblingLookup is not null);
    }

    private static TypeDescriptor GenerateTypeDescriptor(ITypeSymbol symbol, SemanticModel semanticModel)
    {
        var properties = symbol.GetMembers()
                               .OfType<IPropertySymbol>()
                               .Where(m => m is { DeclaredAccessibility: Accessibility.Public, IsStatic: false })
                               .Select(p => new PropertyDescriptor(p.Name,
                                                                   p.Type.ToString(),
                                                                   IsPrimitive(p.Type),
                                                                   IsNullable(p.Type),
                                                                   p.Type.SpecialType == SpecialType.System_String,
                                                                   GenerateEnumerableDescriptor(p.Type)))
                               .ToArray();

        // TODO: improve the logic for finding own properties to account for fields, etc.
        return new(
            symbol.Name,
            symbol.ContainingNamespace?.IsGlobalNamespace ?? false ? string.Empty : symbol.ContainingNamespace?.ToString() ?? string.Empty,
            symbol.ToString(),
            symbol.DeclaredAccessibility,
            symbol.IsRecord,
            symbol.GetMembers().OfType<IPropertySymbol>().Any(p => p.Name != "EqualityContract"),
            ParentClasses: GetParentClasses(symbol, semanticModel),
            Properties: new(properties),
            Enumerable: GenerateEnumerableDescriptor(symbol));
    }

    private static TypeDescriptor GenerateUnitResponseTypeDescriptor()
    {
        return new(
            "UnitMessageResponse",
            "Conqueror",
            "Conqueror.UnitMessageResponse",
            Accessibility.Public,
            true,
            false,
            ParentClasses: default,
            Properties: default,
            Enumerable: null);
    }

    private static EnumerableDescriptor? GenerateEnumerableDescriptor(ITypeSymbol symbol)
    {
        if (symbol.SpecialType == SpecialType.System_String)
        {
            return null;
        }

        foreach (var interfaceType in symbol.AllInterfaces)
        {
            if (interfaceType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            {
                var typeArgument = interfaceType.TypeArguments[0];

                return new(symbol.ToString(),
                           typeArgument.ToString(),
                           symbol is IArrayTypeSymbol,
                           IsPrimitive(typeArgument),
                           typeArgument.IsReferenceType);
            }
        }

        return null;
    }

    private static EquatableArray<ParentClass> GetParentClasses(ITypeSymbol symbol, SemanticModel semanticModel)
    {
        if (symbol.DeclaringSyntaxReferences.Length == 0)
        {
            return [];
        }

        var syntaxNode = symbol.DeclaringSyntaxReferences[0].GetSyntax();

        var parentSyntax = syntaxNode.Parent as TypeDeclarationSyntax;

        var result = new List<ParentClass>();

        while (parentSyntax != null && IsAllowedKind(parentSyntax.Kind()))
        {
            var parentSymbol = semanticModel.GetDeclaredSymbol(parentSyntax);

            var parentClassInfo = new ParentClass(
                parentSyntax.Keyword.ValueText,
                parentSyntax.Identifier.ToString() + parentSyntax.TypeParameterList,
                parentSymbol?.DeclaredAccessibility ?? Accessibility.NotApplicable);

            result.Insert(0, parentClassInfo);

            parentSyntax = parentSyntax.Parent as TypeDeclarationSyntax;
        }

        return new([.. result]);
    }

    private static bool IsAllowedKind(SyntaxKind kind) =>
        kind is SyntaxKind.ClassDeclaration
            or SyntaxKind.StructDeclaration
            or SyntaxKind.RecordDeclaration;

    private static bool IsPrimitive(ITypeSymbol symbol) =>
        symbol.SpecialType
            is SpecialType.System_String
            or SpecialType.System_Boolean
            or SpecialType.System_Byte
            or SpecialType.System_Int16
            or SpecialType.System_Int32
            or SpecialType.System_Int64
            or SpecialType.System_Single
            or SpecialType.System_Double
            or SpecialType.System_Decimal;

    private static bool IsNullable(ITypeSymbol symbol) =>
        symbol.IsReferenceType;
}
