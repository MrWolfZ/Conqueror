namespace Conqueror.SourceGenerators.Util;

internal interface IHasDiagnostics
{
    EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics { get; }
}
