namespace Conqueror.SourceGenerators.Signalling;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct SignalTypeDescriptor(
    TypeDescriptor SignalDescriptor,
    EquatableArray<SignalAttributeDescriptor> Attributes,
    bool HasJsonSerializerContext
)
{
    public readonly EquatableArray<SignalAttributeDescriptor> Attributes = Attributes;
    public readonly bool HasJsonSerializerContext = HasJsonSerializerContext;
    public readonly TypeDescriptor SignalDescriptor = SignalDescriptor;
}

[StructLayout(LayoutKind.Auto)]
internal readonly record struct SignalAttributeDescriptor(
    string Prefix,
    string Namespace,
    string? FullyQualifiedSignalTypeName,
    EquatableArray<AttributeParameterDescriptor> Properties
)
{
    public readonly string? FullyQualifiedSignalTypeName = FullyQualifiedSignalTypeName;
    public readonly string Namespace = Namespace;
    public readonly string Prefix = Prefix;
    public readonly EquatableArray<AttributeParameterDescriptor> Properties = Properties;
}
