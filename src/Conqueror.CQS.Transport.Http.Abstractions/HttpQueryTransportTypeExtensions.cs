namespace Conqueror;

public static class HttpQueryTransportTypeExtensions
{
    public static bool IsHttp(this QueryTransportType transportClientType) => transportClientType.Name == HttpConstants.TransportName;
}
