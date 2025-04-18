namespace Conqueror.SourceGenerators.Util;

public readonly record struct AttributeDescriptor(
    string Name,
    string Namespace,
    string FullyQualifiedName,
    EquatableArray<BaseAttributeTypeDescriptor> BaseTypes)
{
    public readonly EquatableArray<BaseAttributeTypeDescriptor> BaseTypes = BaseTypes;
    public readonly string FullyQualifiedName = FullyQualifiedName;
    public readonly string Name = Name;
    public readonly string Namespace = Namespace;
}

public readonly record struct BaseAttributeTypeDescriptor(
    string Name,
    string Namespace,
    string FullyQualifiedName)
{
    public readonly string FullyQualifiedName = FullyQualifiedName;
    public readonly string Name = Name;
    public readonly string Namespace = Namespace;
}
