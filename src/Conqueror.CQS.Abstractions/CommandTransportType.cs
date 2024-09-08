namespace Conqueror;

public sealed record CommandTransportType(string Name, CommandTransportRole Role);

public enum CommandTransportRole
{
    Client,
    Server,
}
