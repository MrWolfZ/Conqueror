using Conqueror.SourceGenerators.Util;

namespace Conqueror.SourceGenerators.Signalling;

public readonly record struct SignalHandlerTypeDescriptor(
    TypeDescriptor HandlerDescriptor,
    EquatableArray<SignalTypeDescriptor> SignalTypes)
{
    public readonly TypeDescriptor HandlerDescriptor = HandlerDescriptor;
    public readonly EquatableArray<SignalTypeDescriptor> SignalTypes = SignalTypes;
}
