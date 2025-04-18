namespace Conqueror.SourceGenerators.Util.Eventing;

public readonly record struct EventNotificationTypesDescriptor(
    TypeDescriptor EventNotificationTypeDescriptor,
    bool HasJsonSerializerContext)
{
    public readonly TypeDescriptor EventNotificationTypeDescriptor = EventNotificationTypeDescriptor;
    public readonly bool HasJsonSerializerContext = HasJsonSerializerContext;
}
