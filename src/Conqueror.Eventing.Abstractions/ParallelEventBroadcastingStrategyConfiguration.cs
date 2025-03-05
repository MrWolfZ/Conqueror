using System;

namespace Conqueror;

public sealed record ParallelEventBroadcastingStrategyConfiguration
{
    private int? maxDegreeOfParallelism;

    public int? MaxDegreeOfParallelism
    {
        get => maxDegreeOfParallelism;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentException($"maximum degree of parallelism for parallel in-memory publishing must be a positive integer, but was {value}");
            }

            maxDegreeOfParallelism = value;
        }
    }
}
