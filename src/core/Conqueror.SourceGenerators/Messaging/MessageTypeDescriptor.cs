namespace Conqueror.SourceGenerators.Messaging;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct MessageTypeDescriptor(
    TypeDescriptor MessageDescriptor,
    TypeDescriptor ResponseDescriptor,
    EquatableArray<MessageAttributeDescriptor> Attributes,
    bool HasJsonSerializerContext,
    EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics
) : IHasDiagnostics
{
    public readonly EquatableArray<MessageAttributeDescriptor> Attributes = Attributes;
    public readonly EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics = Diagnostics;
    public readonly bool HasJsonSerializerContext = HasJsonSerializerContext;
    public readonly TypeDescriptor MessageDescriptor = MessageDescriptor;
    public readonly TypeDescriptor ResponseDescriptor = ResponseDescriptor;

    EquatableArray<DiagnosticWithLocationDescriptor> IHasDiagnostics.Diagnostics => Diagnostics;
}

[StructLayout(LayoutKind.Auto)]
internal readonly record struct MessageAttributeDescriptor(
    string Prefix,
    string Namespace,
    string? FullyQualifiedMessageTypeName,
    EquatableArray<AttributeParameterDescriptor> Properties
)
{
    public readonly string? FullyQualifiedMessageTypeName = FullyQualifiedMessageTypeName;
    public readonly string Namespace = Namespace;
    public readonly string Prefix = Prefix;
    public readonly EquatableArray<AttributeParameterDescriptor> Properties = Properties;
}
