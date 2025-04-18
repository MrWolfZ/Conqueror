// ReSharper disable once CheckNamespace
namespace Conqueror;

public static class InProcessEventNotificationTransportTypeExtensions
{
    public static bool IsInProcess(this EventNotificationTransportType transportType) => transportType.Name == ConquerorConstants.InProcessTransportName;
}
