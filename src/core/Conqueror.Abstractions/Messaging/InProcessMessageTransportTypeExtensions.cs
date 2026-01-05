namespace Conqueror;

public static class InProcessMessageTransportTypeExtensions
{
    public static bool IsInProcess(this MessageTransportType transportType) =>
        string.Equals(transportType.Name, ConquerorConstants.InProcessTransportName, StringComparison.Ordinal);
}
