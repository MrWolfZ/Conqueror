namespace Conqueror.SourceGenerators.Util.Messaging;

public readonly record struct MessageTypesDescriptor(
    TypeDescriptor MessageTypeDescriptor,
    TypeDescriptor ResponseTypeDescriptor)
{
    public readonly TypeDescriptor MessageTypeDescriptor = MessageTypeDescriptor;
    public readonly TypeDescriptor ResponseTypeDescriptor = ResponseTypeDescriptor;
}
