namespace Conqueror.SourceGenerators.Util;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[SuppressMessage(
    "Design",
    "MA0045:Do not use blocking calls in a sync method (need to make calling method async)",
    Justification = "we want the code to be sync"
)]
internal static class GeneratorHelper
{
    public static TypeDescriptor GenerateTypeDescriptor(ITypeSymbol symbol, SemanticModel semanticModel)
    {
        var typeArguments = (symbol as INamedTypeSymbol)?.TypeArguments ?? ImmutableArray<ITypeSymbol>.Empty;

        // TODO: improve the logic for finding own properties to account for fields, etc.
        return new TypeDescriptor(
            symbol.Name,
            symbol.Name
                + (
                    typeArguments.Length > 0
                        ? "<" + string.Join(", ", typeArguments.Select(t => t.ToString())) + ">"
                        : ""
                ),
            symbol.ContainingNamespace?.IsGlobalNamespace ?? false ? "" : symbol.ContainingNamespace?.ToString() ?? "",
            symbol.ToString(),
            symbol.DeclaredAccessibility,
            symbol.IsRecord,
            symbol.IsAbstract,
            IsPrimitive(symbol),
            new(typeArguments.Select(t => GenerateTypeDescriptor(t, semanticModel).ToWrapper()).ToArray()),
            GetTypeConstraints(symbol as INamedTypeSymbol),
            GetAttributes(symbol),
            GetProperties(symbol, semanticModel),
            GetMethods(symbol),
            GetBaseTypes(symbol, semanticModel),
            GetInterfaces(symbol),
            GetParentClasses(symbol, semanticModel),
            GenerateEnumerableDescriptor(symbol, semanticModel),
            GetTupleDescriptor(symbol, semanticModel)
        );
    }

    public static EquatableArray<AttributeParameterDescriptor> GetAttributeProperties(AttributeData attributeData)
    {
        var result = new AttributeParameterDescriptor[attributeData.NamedArguments.Length];

        for (var i = 0; i < attributeData.NamedArguments.Length; i += 1)
        {
            var namedArgument = attributeData.NamedArguments[i];
            result[i] = new AttributeParameterDescriptor(
                namedArgument.Key,
                namedArgument.Value.Type?.ToString() ?? "object?",
                namedArgument.Value.Kind is TypedConstantKind.Array,
                namedArgument.Value.Kind is TypedConstantKind.Primitive,
                GetValue(namedArgument.Value)
            );
        }

        return new EquatableArray<AttributeParameterDescriptor>(result);

        AttributeParameterValueDescriptor GetValue(TypedConstant value)
        {
            return value.Kind is TypedConstantKind.Array
                ? GetArrayValue(value.Values)
                : new(value.Value, Values: null, value.IsNull);
        }

        AttributeParameterValueDescriptor GetArrayValue(ImmutableArray<TypedConstant> values)
        {
            return new AttributeParameterValueDescriptor(
                Value: null,
                new(values.Select(GetValue).ToArray()),
                IsNull: false
            );
        }
    }

    private static EnumerableDescriptor? GenerateEnumerableDescriptor(ITypeSymbol symbol, SemanticModel semanticModel)
    {
        if (symbol.SpecialType is SpecialType.System_String)
        {
            return null;
        }

        foreach (var interfaceType in symbol.AllInterfaces)
        {
            if (interfaceType.OriginalDefinition.SpecialType is SpecialType.System_Collections_Generic_IEnumerable_T)
            {
                var typeArgument = interfaceType.TypeArguments[index: 0];

                return new EnumerableDescriptor(
                    symbol.ToString(),
                    symbol is IArrayTypeSymbol,
                    GenerateTypeDescriptor(typeArgument, semanticModel).ToWrapper()
                );
            }
        }

        return null;
    }

    private static TupleDescriptor? GetTupleDescriptor(ITypeSymbol symbol, SemanticModel semanticModel)
    {
        if (!symbol.IsTupleType || symbol is not INamedTypeSymbol s)
        {
            return null;
        }

        var items = s.TypeArguments.Select(a => GenerateTypeDescriptor(a, semanticModel).ToWrapper()).ToArray();

        return new TupleDescriptor(new(items));
    }

    private static EquatableArray<BaseTypeDescriptor> GetBaseTypes(ITypeSymbol symbol, SemanticModel semanticModel)
    {
        var baseType = symbol.BaseType;

        var result = new List<BaseTypeDescriptor>();

        while (
            baseType is not null && !string.Equals(baseType.ToString(), "object", StringComparison.OrdinalIgnoreCase)
        )
        {
            result.Add(
                new(
                    baseType.Name,
                    baseType.ContainingNamespace?.ToString() ?? "",
                    baseType.ToString(),
                    GetAttributes(baseType),
                    GetProperties(baseType, semanticModel)
                )
            );

            baseType = baseType.BaseType;
        }

        return new EquatableArray<BaseTypeDescriptor>([.. result]);
    }

    private static EquatableArray<InterfaceDescriptor> GetInterfaces(ITypeSymbol symbol)
    {
        return new EquatableArray<InterfaceDescriptor>(
            GetInterfacesInner(symbol)
                .Distinct(SymbolEqualityComparer.Default)
                .OfType<INamedTypeSymbol>()
                .Select(i => new InterfaceDescriptor(i.Name, i.ContainingNamespace?.ToString() ?? "", i.ToString()))
                .ToArray()
        );

        static IEnumerable<INamedTypeSymbol> GetInterfacesInner(ITypeSymbol s)
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
        if (symbol.DeclaringSyntaxReferences.Length is 0)
        {
            return EquatableArray<ParentClass>.Empty;
        }

        var syntaxNode = symbol.DeclaringSyntaxReferences[index: 0].GetSyntax(CancellationToken.None);

        var parentSyntax = syntaxNode.Parent as TypeDeclarationSyntax;

        var result = new List<ParentClass>();

        while (parentSyntax is not null && IsAllowedKind(parentSyntax.Kind()))
        {
            var parentSymbol = semanticModel.GetDeclaredSymbolSafe(parentSyntax);

            var parentClassInfo = new ParentClass(
                parentSyntax.Keyword.ValueText,
                string.Concat(parentSyntax.Identifier.ToString(), parentSyntax.TypeParameterList),
                parentSymbol?.DeclaredAccessibility ?? Accessibility.NotApplicable
            );

            result.Insert(index: 0, parentClassInfo);

            parentSyntax = parentSyntax.Parent as TypeDeclarationSyntax;
        }

        return new EquatableArray<ParentClass>([.. result]);

        static bool IsAllowedKind(SyntaxKind kind)
        {
            return kind is SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.RecordDeclaration;
        }
    }

    private static EquatableArray<AttributeDescriptor> GetAttributes(ITypeSymbol symbol)
    {
        return new(
            symbol
                .GetAttributes()
                // filter out system attributes (specifically `AttributeUsageAttribute`) to prevent issues like infinite loops
                .Where(a =>
                    !(a.AttributeClass?.ContainingNamespace.ToString() ?? "").StartsWith(
                        "System",
                        StringComparison.Ordinal
                    )
                )
                .Select(a => new AttributeDescriptor(
                    a.AttributeClass!.Name,
                    a.AttributeClass.ContainingNamespace?.ToString() ?? "",
                    a.AttributeClass.ToString(),
                    GetAttributes(a.AttributeClass)
                ))
                .ToArray()
        );
    }

    private static EquatableArray<PropertyDescriptor> GetProperties(ITypeSymbol symbol, SemanticModel semanticModel)
    {
        var properties = symbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(m =>
                m is { DeclaredAccessibility: Accessibility.Public, IsStatic: false }
                && !string.Equals(m.Name, "EqualityContract", StringComparison.Ordinal)
            )
            .Select(p => new PropertyDescriptor(
                p.Name,
                p.Type.ToString(),
                IsPrimitive(p.Type),
                IsNullable(p.Type),
                p.Type.SpecialType is SpecialType.System_String,
                GenerateEnumerableDescriptor(p.Type, semanticModel)
            ))
            .ToArray();

        return new EquatableArray<PropertyDescriptor>(properties);
    }

    private static EquatableArray<MethodDescriptor> GetMethods(ITypeSymbol symbol)
    {
        var methods = symbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Select(m => new MethodDescriptor(m.Name, m.ReturnType.ToString()))
            .ToArray();

        return new EquatableArray<MethodDescriptor>(methods);
    }

    private static string? GetTypeConstraints(INamedTypeSymbol? symbol)
    {
        if (
            symbol?.DeclaringSyntaxReferences.Length is 0
            || symbol?.DeclaringSyntaxReferences[index: 0].GetSyntax(CancellationToken.None)
                is not TypeDeclarationSyntax s
            || s.ConstraintClauses.Count is 0
        )
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

    private static bool IsNullable(ITypeSymbol symbol) => symbol.IsReferenceType;
}
