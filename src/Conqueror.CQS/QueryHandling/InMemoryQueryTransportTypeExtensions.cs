namespace Conqueror.CQS.QueryHandling;

public static class InMemoryQueryTransportTypeExtensions
{
    public const string TransportName = "in-memory";

    public static bool IsInMemory(this QueryTransportType transportClientType) => transportClientType.Name == TransportName;
}
