namespace Conqueror.SourceGenerators.Util;

public readonly record struct EnumerableDescriptor(
    string FullyQualifiedTypeName,
    string FullyQualifiedItemTypeName,
    bool IsArray,
    bool IsPrimitive,
    bool IsNullable)
{
    public readonly string FullyQualifiedItemTypeName = FullyQualifiedItemTypeName;
    public readonly string FullyQualifiedTypeName = FullyQualifiedTypeName;
    public readonly bool IsArray = IsArray;
    public readonly bool IsNullable = IsNullable;
    public readonly bool IsPrimitive = IsPrimitive;
}
