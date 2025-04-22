namespace Conqueror.SourceGenerators.Util;

public readonly record struct AttributeDescriptor(
    string Name,
    string Namespace,
    string FullyQualifiedName,
    EquatableArray<AttributeDescriptor> Attributes)
{
    public readonly EquatableArray<AttributeDescriptor> Attributes = Attributes;
    public readonly string FullyQualifiedName = FullyQualifiedName;
    public readonly string Name = Name;
    public readonly string Namespace = Namespace;
}

public readonly record struct AttributeParameterDescriptor(
    string Name,
    string FullyQualifiedTypeName,
    bool IsArray,
    bool IsPrimitive,
    AttributeParameterValueDescriptor Value)
{
    public readonly string FullyQualifiedTypeName = FullyQualifiedTypeName;
    public readonly bool IsArray = IsArray;
    public readonly bool IsPrimitive = IsPrimitive;
    public readonly string Name = Name;
    public readonly AttributeParameterValueDescriptor Value = Value;
}

public readonly record struct AttributeParameterValueDescriptor(
    object? Value,
    EquatableArray<AttributeParameterValueDescriptor>? Values,
    bool IsNull)
{
    public readonly bool IsNull = IsNull;
    public readonly object? Value = Value;
    public readonly EquatableArray<AttributeParameterValueDescriptor>? Values = Values;
}
