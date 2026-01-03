namespace Conqueror;

public static class InProcessIteratorTransportTypeExtensions
{
    public static bool IsInProcess(this IteratorTransportType transportType) =>
        string.Equals(transportType.Name, ConquerorConstants.InProcessTransportName, StringComparison.Ordinal);
}
