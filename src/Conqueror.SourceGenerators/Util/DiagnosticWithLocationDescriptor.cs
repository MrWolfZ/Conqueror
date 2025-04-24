using Microsoft.CodeAnalysis;

namespace Conqueror.SourceGenerators.Util;

public readonly record struct DiagnosticWithLocationDescriptor(
    DiagnosticDescriptor Diagnostic,
    LocationDescriptor? Location)
{
    public readonly DiagnosticDescriptor Diagnostic = Diagnostic;
    public readonly LocationDescriptor? Location = Location;
}
