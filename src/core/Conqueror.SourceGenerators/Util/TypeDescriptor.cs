namespace Conqueror.SourceGenerators.Util;

using Microsoft.CodeAnalysis;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct TypeDescriptor(
    string SimpleName, // name without generic arguments
    string Name,
    string Namespace,
    string FullyQualifiedName,
    Accessibility Accessibility,
    bool IsRecord,
    bool IsAbstract,
    bool IsPrimitive,
    EquatableArray<TypeDescriptorWrapper> TypeArguments,
    string? TypeConstraints,
    EquatableArray<AttributeDescriptor> Attributes,
    EquatableArray<PropertyDescriptor> Properties,
    EquatableArray<MethodDescriptor> Methods,
    EquatableArray<BaseTypeDescriptor> BaseTypes,
    EquatableArray<InterfaceDescriptor> Interfaces,
    EquatableArray<ParentClass> ParentClasses,
    EnumerableDescriptor? Enumerable,
    TupleDescriptor? Tuple
)
{
    public readonly Accessibility Accessibility = Accessibility;
    public readonly EquatableArray<AttributeDescriptor> Attributes = Attributes;
    public readonly EquatableArray<BaseTypeDescriptor> BaseTypes = BaseTypes;
    public readonly EnumerableDescriptor? Enumerable = Enumerable;
    public readonly string FullyQualifiedName = FullyQualifiedName;
    public readonly EquatableArray<InterfaceDescriptor> Interfaces = Interfaces;
    public readonly bool IsAbstract = IsAbstract;
    public readonly bool IsPrimitive = IsPrimitive;
    public readonly bool IsRecord = IsRecord;
    public readonly EquatableArray<MethodDescriptor> Methods = Methods;
    public readonly string Name = Name;
    public readonly string Namespace = Namespace;
    public readonly EquatableArray<ParentClass> ParentClasses = ParentClasses;
    public readonly EquatableArray<PropertyDescriptor> Properties = Properties;
    public readonly string SimpleName = SimpleName;
    public readonly TupleDescriptor? Tuple = Tuple;
    public readonly EquatableArray<TypeDescriptorWrapper> TypeArguments = TypeArguments;
    public readonly string? TypeConstraints = TypeConstraints;

    public TypeDescriptorWrapper ToWrapper() => new(this);
}

// reference type to break cycle that would otherwise cause the generator to fail to initialize
internal sealed record TypeDescriptorWrapper(TypeDescriptor Descriptor)
{
    public TypeDescriptor Descriptor { get; } = Descriptor;
}
