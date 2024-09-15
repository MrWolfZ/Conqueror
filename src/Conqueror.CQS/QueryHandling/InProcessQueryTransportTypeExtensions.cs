namespace Conqueror.CQS.QueryHandling;

public static class InProcessQueryTransportTypeExtensions
{
    public const string TransportName = "in-process";

    public static bool IsInProcess(this QueryTransportType transportClientType) => transportClientType.Name == TransportName;
}
