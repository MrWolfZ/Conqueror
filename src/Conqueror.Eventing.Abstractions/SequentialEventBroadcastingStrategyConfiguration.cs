namespace Conqueror;

public sealed record SequentialEventBroadcastingStrategyConfiguration
{
    public SequentialEventBroadcastingStrategyExceptionHandling ExceptionHandling { get; set; } = SequentialEventBroadcastingStrategyExceptionHandling.ThrowOnFirstException;
}

public enum SequentialEventBroadcastingStrategyExceptionHandling
{
    ThrowOnFirstException,
    ThrowAfterAll,
}
