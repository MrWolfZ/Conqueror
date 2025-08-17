namespace Conqueror;

public static class InProcessSignalTransportTypeExtensions
{
    public static bool IsInProcess(this SignalTransportType transportType) =>
        string.Equals(transportType.Name, ConquerorConstants.InProcessTransportName, StringComparison.Ordinal);
}
