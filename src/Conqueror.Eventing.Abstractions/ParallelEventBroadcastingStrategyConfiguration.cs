namespace Conqueror;

public sealed record ParallelEventBroadcastingStrategyConfiguration
{
    public int? MaxDegreeOfParallelism { get; set; }
}
