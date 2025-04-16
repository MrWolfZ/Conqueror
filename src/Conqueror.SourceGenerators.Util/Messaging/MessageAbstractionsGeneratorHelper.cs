﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Conqueror.SourceGenerators.Util.Messaging;

public static class MessageAbstractionsGeneratorHelper
{
    public static MessageTypesDescriptor GenerateMessageTypesDescriptor(INamedTypeSymbol messageTypeSymbol,
                                                                        INamedTypeSymbol? responseTypeSymbol)
    {
        var messageTypeDescriptor = GenerateTypeDescriptor(messageTypeSymbol);

        return new(messageTypeDescriptor, responseTypeSymbol is not null ? GenerateTypeDescriptor(responseTypeSymbol) : GenerateUnitResponseTypeDescriptor());
    }

    private static TypeDescriptor GenerateTypeDescriptor(INamedTypeSymbol symbol)
    {
        var typeSyntax = symbol.DeclaringSyntaxReferences[0].GetSyntax();

        var properties = symbol.GetMembers()
                               .OfType<IPropertySymbol>()
                               .Where(m => m is { DeclaredAccessibility: Accessibility.Public, IsStatic: false })
                               .Select(p => new PropertyDescriptor(p.Name,
                                                                   p.Type.ToString(),
                                                                   IsPrimitive(p.Type),
                                                                   IsNullable(p.Type),
                                                                   p.Type.SpecialType == SpecialType.System_String,
                                                                   GenerateEnumerableDescriptor(p)))
                               .ToArray();

        // TODO: improve the logic for finding own properties to account for fields, etc.
        return new(
            symbol.Name,
            symbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : symbol.ContainingNamespace.ToString(),
            symbol.ToString(),
            symbol.IsRecord,
            symbol.GetMembers().OfType<IPropertySymbol>().Any(p => p.Name != "EqualityContract"),
            ParentClasses: GetParentClasses(typeSyntax),
            Properties: new(properties));
    }

    private static TypeDescriptor GenerateUnitResponseTypeDescriptor()
    {
        return new(
            "UnitMessageResponse",
            "Conqueror",
            "Conqueror.UnitMessageResponse",
            true,
            false,
            ParentClasses: default,
            Properties: default);
    }

    private static EnumerableDescriptor? GenerateEnumerableDescriptor(IPropertySymbol symbol)
    {
        if (symbol.Type.SpecialType == SpecialType.System_String)
        {
            return null;
        }

        foreach (var interfaceType in symbol.Type.AllInterfaces)
        {
            if (interfaceType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            {
                var typeArgument = interfaceType.TypeArguments[0];

                return new(symbol.Type.ToString(),
                           typeArgument.ToString(),
                           symbol.Type is IArrayTypeSymbol,
                           IsPrimitive(typeArgument),
                           typeArgument.IsReferenceType);
            }
        }

        return null;
    }

    private static EquatableArray<ParentClass> GetParentClasses(SyntaxNode syntaxNode)
    {
        var parentSyntax = syntaxNode.Parent as TypeDeclarationSyntax;

        var result = new List<ParentClass>();

        while (parentSyntax != null && IsAllowedKind(parentSyntax.Kind()))
        {
            var parentClassInfo = new ParentClass(
                parentSyntax.Keyword.ValueText,
                parentSyntax.Identifier.ToString() + parentSyntax.TypeParameterList);

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
