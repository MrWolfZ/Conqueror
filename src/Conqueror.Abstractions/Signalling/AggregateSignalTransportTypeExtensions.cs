using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public static class AggregateSignalTransportTypeExtensions
{
    public static bool IsAggregate(this SignalTransportType transportType) => transportType.Name.StartsWith($"{ConquerorConstants.AggregateTransportName}[");

    public static bool IsAggregate(this SignalTransportType transportType, [NotNullWhen(true)] out string[]? innerTransportTypes)
    {
        innerTransportTypes = null;

        if (!transportType.IsAggregate())
        {
            return false;
        }

        innerTransportTypes = transportType.Name[(ConquerorConstants.AggregateTransportName.Length + 1)..^1].Split(',');

        return true;
    }
}
