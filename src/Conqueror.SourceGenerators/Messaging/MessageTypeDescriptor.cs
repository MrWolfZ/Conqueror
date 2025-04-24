using Conqueror.SourceGenerators.Util;

namespace Conqueror.SourceGenerators.Messaging;

public readonly record struct MessageTypeDescriptor(
    TypeDescriptor MessageDescriptor,
    TypeDescriptor ResponseDescriptor,
    EquatableArray<MessageAttributeDescriptor> Attributes,
    bool HasJsonSerializerContext,
    EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics)
    : IHasDiagnostics
{
    public readonly EquatableArray<MessageAttributeDescriptor> Attributes = Attributes;
    public readonly EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics = Diagnostics;
    public readonly bool HasJsonSerializerContext = HasJsonSerializerContext;
    public readonly TypeDescriptor MessageDescriptor = MessageDescriptor;
    public readonly TypeDescriptor ResponseDescriptor = ResponseDescriptor;

    EquatableArray<DiagnosticWithLocationDescriptor> IHasDiagnostics.Diagnostics => Diagnostics;
}

public readonly record struct MessageAttributeDescriptor(
    string Prefix,
    string Namespace,
    EquatableArray<AttributeParameterDescriptor> Properties)
{
    public readonly string Namespace = Namespace;
    public readonly string Prefix = Prefix;
    public readonly EquatableArray<AttributeParameterDescriptor> Properties = Properties;
}
