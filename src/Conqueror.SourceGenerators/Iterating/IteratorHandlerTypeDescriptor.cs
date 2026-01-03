namespace Conqueror.SourceGenerators.Iterating;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct IteratorHandlerTypeDescriptor(
    TypeDescriptor HandlerDescriptor,
    EquatableArray<IteratorTypeDescriptor> IteratorTypes,
    EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics
) : IHasDiagnostics
{
    public readonly EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics = Diagnostics;
    public readonly TypeDescriptor HandlerDescriptor = HandlerDescriptor;
    public readonly EquatableArray<IteratorTypeDescriptor> IteratorTypes = IteratorTypes;

    EquatableArray<DiagnosticWithLocationDescriptor> IHasDiagnostics.Diagnostics => Diagnostics;
}
