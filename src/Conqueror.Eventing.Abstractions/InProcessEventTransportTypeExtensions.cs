namespace Conqueror;

public static class InProcessEventTransportTypeExtensions
{
    public static bool IsInProcess(this EventTransportType transportType) => transportType.Name == InProcessEventAttribute.TransportName;
}
