namespace Conqueror.SourceGenerators.Util;

public readonly record struct EnumerableDescriptor(
    string FullyQualifiedTypeName,
    bool IsArray,
    TypeDescriptorWrapper ItemType)
{
    public readonly string FullyQualifiedTypeName = FullyQualifiedTypeName;
    public readonly bool IsArray = IsArray;
    public readonly TypeDescriptorWrapper ItemType = ItemType;
}
