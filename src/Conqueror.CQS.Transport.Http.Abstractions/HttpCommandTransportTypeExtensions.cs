namespace Conqueror;

public static class HttpCommandTransportTypeExtensions
{
    public static bool IsHttp(this CommandTransportType transportType) => transportType.Name == HttpConstants.TransportName;
}
