namespace Conqueror.SourceGenerators.Util;

public readonly record struct BaseTypeDescriptor(
    string Name,
    string Namespace,
    string FullyQualifiedName,
    EquatableArray<AttributeDescriptor> Attributes,
    EquatableArray<PropertyDescriptor> Properties)
{
    public readonly EquatableArray<AttributeDescriptor> Attributes = Attributes;
    public readonly string FullyQualifiedName = FullyQualifiedName;
    public readonly string Name = Name;
    public readonly string Namespace = Namespace;
    public readonly EquatableArray<PropertyDescriptor> Properties = Properties;
}
