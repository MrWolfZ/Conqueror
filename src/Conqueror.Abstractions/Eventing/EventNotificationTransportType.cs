// ReSharper disable once CheckNamespace
namespace Conqueror;

public sealed record EventNotificationTransportType(string Name, EventNotificationTransportRole Role);

public enum EventNotificationTransportRole
{
    Publisher,
    Receiver,
}
