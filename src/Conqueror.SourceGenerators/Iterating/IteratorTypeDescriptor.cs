namespace Conqueror.SourceGenerators.Iterating;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct IteratorTypeDescriptor(
    TypeDescriptor IteratorDescriptor,
    TypeDescriptor ItemDescriptor,
    EquatableArray<IteratorAttributeDescriptor> Attributes,
    bool HasJsonSerializerContext,
    EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics
) : IHasDiagnostics
{
    public readonly EquatableArray<IteratorAttributeDescriptor> Attributes = Attributes;
    public readonly EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics = Diagnostics;
    public readonly bool HasJsonSerializerContext = HasJsonSerializerContext;
    public readonly TypeDescriptor IteratorDescriptor = IteratorDescriptor;
    public readonly TypeDescriptor ItemDescriptor = ItemDescriptor;

    EquatableArray<DiagnosticWithLocationDescriptor> IHasDiagnostics.Diagnostics => Diagnostics;
}

[StructLayout(LayoutKind.Auto)]
internal readonly record struct IteratorAttributeDescriptor(
    string Prefix,
    string Namespace,
    string? FullyQualifiedIteratorTypeName,
    EquatableArray<AttributeParameterDescriptor> Properties
)
{
    public readonly string? FullyQualifiedIteratorTypeName = FullyQualifiedIteratorTypeName;
    public readonly string Namespace = Namespace;
    public readonly string Prefix = Prefix;
    public readonly EquatableArray<AttributeParameterDescriptor> Properties = Properties;
}
