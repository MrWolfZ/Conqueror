namespace Conqueror.SourceGenerators.Util;

public readonly record struct InterfaceDescriptor(
    string Name,
    string Namespace,
    string FullyQualifiedName)
{
    public readonly string FullyQualifiedName = FullyQualifiedName;
    public readonly string Name = Name;
    public readonly string Namespace = Namespace;
}
