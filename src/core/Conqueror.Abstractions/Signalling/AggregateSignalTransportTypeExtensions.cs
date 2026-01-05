namespace Conqueror;

public static class AggregateSignalTransportTypeExtensions
{
    public static bool IsAggregate(this SignalTransportType transportType) =>
        transportType.Name.StartsWith($"{ConquerorConstants.AggregateTransportName}[", StringComparison.Ordinal);

    [SuppressMessage(
        "Critical Code Smell",
        "S3874:\"out\" and \"ref\" parameters should not be used",
        Justification = "the out param is an optional enhancement in this overload"
    )]
    public static bool IsAggregate(
        this SignalTransportType transportType,
        [NotNullWhen(returnValue: true)] out string[]? innerTransportTypes
    )
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
