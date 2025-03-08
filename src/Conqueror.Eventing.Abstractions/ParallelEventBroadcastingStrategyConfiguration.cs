using System;

namespace Conqueror;

public sealed record ParallelEventBroadcastingStrategyConfiguration
{
    public int? MaxDegreeOfParallelism { get; private set; }

    public ParallelEventBroadcastingStrategyConfiguration WithMaxDegreeOfParallelism(int? value)
    {
        if (value <= 0)
        {
            throw new ArgumentException($"maximum degree of parallelism for parallel in-memory publishing must be a positive integer, but was {value}");
        }

        MaxDegreeOfParallelism = value;
        return this;
    }
}
