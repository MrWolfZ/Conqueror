// ReSharper disable once CheckNamespace
namespace Conqueror;

public static class InProcessSignalTransportTypeExtensions
{
    public static bool IsInProcess(this SignalTransportType transportType) => transportType.Name == ConquerorConstants.InProcessTransportName;
}
