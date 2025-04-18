using System;

namespace Conqueror.Eventing;

public sealed record ParallelEventNotificationBroadcastingStrategyConfiguration
{
    public int? MaxDegreeOfParallelism { get; private set; }

    public ParallelEventNotificationBroadcastingStrategyConfiguration WithMaxDegreeOfParallelism(int? value)
    {
        if (value <= 0)
        {
            throw new ArgumentException($"maximum degree of parallelism for parallel in-memory publishing must be a positive integer, but was {value}");
        }

        MaxDegreeOfParallelism = value;
        return this;
    }
}
