namespace Conqueror;

public sealed record EventTransportType(string Name, EventTransportRole Role);

public enum EventTransportRole
{
    Publisher,
    Receiver,
}
