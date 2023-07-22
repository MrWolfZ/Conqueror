namespace Conqueror;

public sealed record SequentialInMemoryEventPublishingStrategyConfiguration
{
    public SequentialInMemoryEventPublishingStrategyExceptionHandling ExceptionHandling { get; set; } = SequentialInMemoryEventPublishingStrategyExceptionHandling.ThrowOnFirstException;
}

public enum SequentialInMemoryEventPublishingStrategyExceptionHandling
{
    ThrowOnFirstException,
    ThrowAfterAll,
}
