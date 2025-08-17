namespace Conqueror.SourceGenerators.Util;

using Microsoft.CodeAnalysis;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct DiagnosticWithLocationDescriptor(
    DiagnosticDescriptor Diagnostic,
    LocationDescriptor? Location
)
{
    public readonly DiagnosticDescriptor Diagnostic = Diagnostic;
    public readonly LocationDescriptor? Location = Location;
}
