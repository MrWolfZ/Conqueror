using Microsoft.CodeAnalysis;

namespace Conqueror.SourceGenerators.Util;

public readonly record struct TypeDescriptor(
    string SimpleName, // name without generic arguments
    string Name,
    string Namespace,
    string FullyQualifiedName,
    Accessibility Accessibility,
    bool IsRecord,
    EquatableArray<string> TypeArguments,
    string? TypeConstraints,
    EquatableArray<AttributeDescriptor> Attributes,
    EquatableArray<PropertyDescriptor> Properties,
    EquatableArray<BaseTypeDescriptor> BaseTypes,
    EquatableArray<InterfaceDescriptor> Interfaces,
    EquatableArray<ParentClass> ParentClasses,
    EnumerableDescriptor? Enumerable)
{
    public readonly Accessibility Accessibility = Accessibility;
    public readonly EquatableArray<AttributeDescriptor> Attributes = Attributes;
    public readonly EquatableArray<BaseTypeDescriptor> BaseTypes = BaseTypes;
    public readonly EnumerableDescriptor? Enumerable = Enumerable;
    public readonly string FullyQualifiedName = FullyQualifiedName;
    public readonly EquatableArray<InterfaceDescriptor> Interfaces = Interfaces;
    public readonly bool IsRecord = IsRecord;
    public readonly string Name = Name;
    public readonly string Namespace = Namespace;
    public readonly EquatableArray<ParentClass> ParentClasses = ParentClasses;
    public readonly EquatableArray<PropertyDescriptor> Properties = Properties;
    public readonly string SimpleName = SimpleName;
    public readonly EquatableArray<string> TypeArguments = TypeArguments;
    public readonly string? TypeConstraints = TypeConstraints;
}

public readonly record struct ParentClass(
    string Keyword,
    string Name,
    Accessibility Accessibility)
{
    public readonly Accessibility Accessibility = Accessibility;
    public readonly string Keyword = Keyword;
    public readonly string Name = Name;
}
