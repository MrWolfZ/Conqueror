namespace Conqueror;

public static class InProcessMessageTransportTypeExtensions
{
    public static bool IsInProcess(this MessageTransportType transportType) => transportType.Name == InProcessMessageTransport.Name;
}
