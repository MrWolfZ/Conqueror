namespace Conqueror.SourceGenerators.Util.Signalling;

public readonly record struct SignalTypesDescriptor(
    TypeDescriptor SignalTypeDescriptor,
    bool HasJsonSerializerContext)
{
    public readonly bool HasJsonSerializerContext = HasJsonSerializerContext;
    public readonly TypeDescriptor SignalTypeDescriptor = SignalTypeDescriptor;
}
