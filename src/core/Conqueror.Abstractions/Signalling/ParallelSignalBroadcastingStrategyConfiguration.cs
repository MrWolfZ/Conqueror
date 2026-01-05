namespace Conqueror.Signalling;

using System.Globalization;

public sealed record ParallelSignalBroadcastingStrategyConfiguration
{
    public int? MaxDegreeOfParallelism { get; private set; }

    public ParallelSignalBroadcastingStrategyConfiguration WithMaxDegreeOfParallelism(int? value)
    {
        if (value <= 0)
        {
            throw new ArgumentException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"maximum degree of parallelism for parallel in-memory publishing must be a positive integer, but was {value}"
                ),
                nameof(value)
            );
        }

        MaxDegreeOfParallelism = value;

        return this;
    }
}
