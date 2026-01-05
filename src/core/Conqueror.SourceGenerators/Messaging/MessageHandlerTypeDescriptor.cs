namespace Conqueror.SourceGenerators.Messaging;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct MessageHandlerTypeDescriptor(
    TypeDescriptor HandlerDescriptor,
    EquatableArray<MessageTypeDescriptor> MessageTypes,
    EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics
) : IHasDiagnostics
{
    public readonly EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics = Diagnostics;
    public readonly TypeDescriptor HandlerDescriptor = HandlerDescriptor;
    public readonly EquatableArray<MessageTypeDescriptor> MessageTypes = MessageTypes;

    EquatableArray<DiagnosticWithLocationDescriptor> IHasDiagnostics.Diagnostics => Diagnostics;
}
