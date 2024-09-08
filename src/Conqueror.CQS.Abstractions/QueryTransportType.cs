namespace Conqueror;

public sealed record QueryTransportType(string Name, QueryTransportRole Role);

public enum QueryTransportRole
{
    Client,
    Server,
}
