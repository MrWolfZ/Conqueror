namespace Conqueror.SourceGenerators.Util;

public interface IHasDiagnostics
{
    EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics { get; }
}
