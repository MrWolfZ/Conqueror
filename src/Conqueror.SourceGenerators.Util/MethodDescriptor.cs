namespace Conqueror.SourceGenerators.Util;

public readonly record struct MethodDescriptor(
    string Name,
    string FullyQualifiedReturnTypeName)
{
    public readonly string FullyQualifiedReturnTypeName = FullyQualifiedReturnTypeName;
    public readonly string Name = Name;
}
