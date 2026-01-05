namespace Conqueror.SourceGenerators.Signalling;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct SignalHandlerTypeDescriptor(
    TypeDescriptor HandlerDescriptor,
    EquatableArray<SignalTypeDescriptor> SignalTypes,
    EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics
) : IHasDiagnostics
{
    public readonly EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics = Diagnostics;
    public readonly TypeDescriptor HandlerDescriptor = HandlerDescriptor;
    public readonly EquatableArray<SignalTypeDescriptor> SignalTypes = SignalTypes;

    EquatableArray<DiagnosticWithLocationDescriptor> IHasDiagnostics.Diagnostics => Diagnostics;
}
