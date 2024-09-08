namespace Conqueror.CQS.CommandHandling;

public static class InMemoryCommandTransportTypeExtensions
{
    public const string TransportName = "in-memory";

    public static bool IsInMemory(this CommandTransportType transportClientType) => transportClientType.Name == TransportName;
}
