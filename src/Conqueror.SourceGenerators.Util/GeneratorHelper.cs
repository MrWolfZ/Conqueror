using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Conqueror.SourceGenerators.Util;

public static class GeneratorHelper
{
    public static TypeDescriptor GenerateTypeDescriptor(ITypeSymbol symbol, SemanticModel semanticModel)
    {
        var typeArguments = (symbol as INamedTypeSymbol)?.TypeArguments ?? [];

        // TODO: improve the logic for finding own properties to account for fields, etc.
        return new(
            symbol.Name,
            symbol.Name + (typeArguments.Length > 0 ? "<" + string.Join(", ", typeArguments.Select(t => t.ToString())) + ">" : string.Empty),
            symbol.ContainingNamespace?.IsGlobalNamespace ?? false ? string.Empty : symbol.ContainingNamespace?.ToString() ?? string.Empty,
            symbol.ToString(),
            symbol.DeclaredAccessibility,
            symbol.IsRecord,
            new(typeArguments.Select(t => t.ToString()).ToArray()),
            GetTypeConstraints(symbol as INamedTypeSymbol),
            GetAttributes(symbol),
            GetProperties(symbol),
            GetBaseTypes(symbol),
            GetInterfaces(symbol),
            GetParentClasses(symbol, semanticModel),
            Enumerable: GenerateEnumerableDescriptor(symbol));
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

    private static EquatableArray<BaseTypeDescriptor> GetBaseTypes(ITypeSymbol symbol)
    {
        var baseType = symbol.BaseType;

        var result = new List<BaseTypeDescriptor>();

        while (baseType != null && baseType.ToString() != "object")
        {
            result.Add(new(baseType.Name,
                           baseType.ContainingNamespace?.ToString() ?? string.Empty,
                           baseType.ToString(),
                           GetAttributes(baseType),
                           GetProperties(baseType)));

            baseType = baseType.BaseType;
        }

        return new([..result]);
    }

    private static EquatableArray<InterfaceDescriptor> GetInterfaces(ITypeSymbol symbol)
    {
        return new(GetInterfacesInner(symbol).Distinct(SymbolEqualityComparer.Default)
                                             .OfType<INamedTypeSymbol>()
                                             .Select(i => new InterfaceDescriptor(i.Name,
                                                                                  i.ContainingNamespace?.ToString() ?? string.Empty,
                                                                                  i.ToString()))
                                             .ToArray());

        IEnumerable<INamedTypeSymbol> GetInterfacesInner(ITypeSymbol s)
        {
            foreach (var i in s.Interfaces)
            {
                yield return i;

                foreach (var nestedInterface in GetInterfacesInner(i))
                {
                    yield return nestedInterface;
                }
            }

            if (s.BaseType is { } baseType)
            {
                foreach (var nestedInterface in GetInterfacesInner(baseType))
                {
                    yield return nestedInterface;
                }
            }
        }
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

        return new([..result]);

        static bool IsAllowedKind(SyntaxKind kind) =>
            kind is SyntaxKind.ClassDeclaration
                or SyntaxKind.StructDeclaration
                or SyntaxKind.RecordDeclaration;
    }

    private static EquatableArray<AttributeDescriptor> GetAttributes(ITypeSymbol symbol)
    {
        return new(symbol.GetAttributes().Where(a => a.AttributeClass is not null)
                         .Select(a => new AttributeDescriptor(a.AttributeClass!.Name,
                                                              a.AttributeClass.ContainingNamespace?.ToString() ?? string.Empty,
                                                              a.AttributeClass.ToString(),
                                                              GetAttributeBaseTypes(a.AttributeClass)))
                         .ToArray());
    }

    private static EquatableArray<BaseAttributeTypeDescriptor> GetAttributeBaseTypes(ITypeSymbol symbol)
    {
        var baseType = symbol.BaseType;

        var result = new List<BaseAttributeTypeDescriptor>();

        while (baseType != null && baseType.ToString() != "object")
        {
            result.Add(new(baseType.Name,
                           baseType.ContainingNamespace?.ToString() ?? string.Empty,
                           baseType.ToString()));

            baseType = baseType.BaseType;
        }

        return new([..result]);
    }

    private static EquatableArray<PropertyDescriptor> GetProperties(ITypeSymbol symbol)
    {
        var properties = symbol.GetMembers()
                               .OfType<IPropertySymbol>()
                               .Where(m => m is { DeclaredAccessibility: Accessibility.Public, IsStatic: false })
                               .Where(m => m.Name != "EqualityContract")
                               .Select(p => new PropertyDescriptor(p.Name,
                                                                   p.Type.ToString(),
                                                                   IsPrimitive(p.Type),
                                                                   IsNullable(p.Type),
                                                                   p.Type.SpecialType == SpecialType.System_String,
                                                                   GenerateEnumerableDescriptor(p.Type)))
                               .ToArray();

        return new(properties);
    }

    private static string? GetTypeConstraints(INamedTypeSymbol? symbol)
    {
        if (symbol?.DeclaringSyntaxReferences.Length == 0
            || symbol?.DeclaringSyntaxReferences[0].GetSyntax() is not TypeDeclarationSyntax s
            || s.ConstraintClauses.Count == 0)
        {
            return null;
        }

        return s.ConstraintClauses.ToFullString();
    }

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
