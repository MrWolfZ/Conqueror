namespace Conqueror.SourceGenerators.Util;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct DescriptorWithDiagnostics<TDescriptor>(
    TDescriptor? Descriptor,
    EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics
)
    where TDescriptor : struct
{
    public readonly TDescriptor? Descriptor = Descriptor;
    public readonly EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics = Diagnostics;
}
