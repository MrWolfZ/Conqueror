namespace Conqueror.SourceGenerators.Util;

public readonly record struct DescriptorWithDiagnostics<TDescriptor>(
    TDescriptor? Descriptor,
    EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics)
    where TDescriptor : struct
{
    public readonly EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics = Diagnostics;
    public readonly TDescriptor? Descriptor = Descriptor;
}
