namespace Conqueror;

public static class HttpCommandTransportTypeExtensions
{
    public static bool IsHttp(this CommandTransportType transportClientType) => transportClientType.Name == HttpConstants.TransportName;
}
