namespace Conqueror.SourceGenerators.Util;

public readonly record struct PropertyDescriptor(
    string Name,
    string FullyQualifiedTypeName,
    bool IsPrimitive,
    bool IsNullable,
    bool IsString,
    EnumerableDescriptor? Enumerable)
{
    public readonly EnumerableDescriptor? Enumerable = Enumerable;
    public readonly string FullyQualifiedTypeName = FullyQualifiedTypeName;
    public readonly bool IsNullable = IsNullable;
    public readonly bool IsPrimitive = IsPrimitive;
    public readonly bool IsString = IsString;
    public readonly string Name = Name;
}
