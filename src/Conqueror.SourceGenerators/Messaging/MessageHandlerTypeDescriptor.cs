using Conqueror.SourceGenerators.Util;

namespace Conqueror.SourceGenerators.Messaging;

public readonly record struct MessageHandlerTypeDescriptor(
    TypeDescriptor HandlerDescriptor,
    EquatableArray<MessageTypeDescriptor> MessageTypes,
    EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics)
    : IHasDiagnostics
{
    public readonly EquatableArray<DiagnosticWithLocationDescriptor> Diagnostics = Diagnostics;
    public readonly TypeDescriptor HandlerDescriptor = HandlerDescriptor;
    public readonly EquatableArray<MessageTypeDescriptor> MessageTypes = MessageTypes;

    EquatableArray<DiagnosticWithLocationDescriptor> IHasDiagnostics.Diagnostics => Diagnostics;
}
