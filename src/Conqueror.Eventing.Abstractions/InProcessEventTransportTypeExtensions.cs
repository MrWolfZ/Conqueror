namespace Conqueror;

public static class InProcessEventTransportTypeExtensions
{
    public const string TransportName = "in-process";

    public static bool IsInProcess(this EventTransportType transportType) => transportType.Name == TransportName;
}
