namespace Conqueror;

public sealed record ParallelInMemoryEventPublishingStrategyConfiguration
{
    public int? MaxDegreeOfParallelism { get; set; }
}
