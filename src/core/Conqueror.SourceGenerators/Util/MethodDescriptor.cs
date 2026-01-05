namespace Conqueror.SourceGenerators.Util;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct MethodDescriptor(string Name, string FullyQualifiedReturnTypeName)
{
    public readonly string FullyQualifiedReturnTypeName = FullyQualifiedReturnTypeName;
    public readonly string Name = Name;
}
