namespace Conqueror;

public sealed record SequentialEventBroadcastingStrategyConfiguration
{
    public ExceptionHandlingStrategy ExceptionHandling { get; private set; } = ExceptionHandlingStrategy.ThrowOnFirstException;

    public SequentialEventBroadcastingStrategyConfiguration WithThrowOnFirstException()
    {
        ExceptionHandling = ExceptionHandlingStrategy.ThrowOnFirstException;
        return this;
    }

    public SequentialEventBroadcastingStrategyConfiguration WithThrowAfterAll()
    {
        ExceptionHandling = ExceptionHandlingStrategy.ThrowAfterAll;
        return this;
    }

    public enum ExceptionHandlingStrategy
    {
        ThrowOnFirstException,
        ThrowAfterAll,
    }
}
