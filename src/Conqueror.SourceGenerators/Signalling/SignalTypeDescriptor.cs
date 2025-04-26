using Conqueror.SourceGenerators.Util;

namespace Conqueror.SourceGenerators.Signalling;

public readonly record struct SignalTypeDescriptor(
    TypeDescriptor SignalDescriptor,
    EquatableArray<SignalAttributeDescriptor> Attributes,
    bool HasJsonSerializerContext)
{
    public readonly EquatableArray<SignalAttributeDescriptor> Attributes = Attributes;
    public readonly bool HasJsonSerializerContext = HasJsonSerializerContext;
    public readonly TypeDescriptor SignalDescriptor = SignalDescriptor;
}

public readonly record struct SignalAttributeDescriptor(
    string Prefix,
    string Namespace,
    string? FullyQualifiedSignalTypeName,
    EquatableArray<AttributeParameterDescriptor> Properties)
{
    public readonly string? FullyQualifiedSignalTypeName = FullyQualifiedSignalTypeName;
    public readonly string Namespace = Namespace;
    public readonly string Prefix = Prefix;
    public readonly EquatableArray<AttributeParameterDescriptor> Properties = Properties;
}
