namespace Conqueror;

public sealed record MessageTransportType(string Name, MessageTransportRole Role);

public enum MessageTransportRole
{
    Client,
    Server,
}
