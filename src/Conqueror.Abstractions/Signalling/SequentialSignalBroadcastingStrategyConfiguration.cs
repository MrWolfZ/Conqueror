namespace Conqueror.Signalling;

public sealed record SequentialSignalBroadcastingStrategyConfiguration
{
    public enum ExceptionHandlingStrategy
    {
        ThrowOnFirstException,
        ThrowAfterAll,
    }

    public ExceptionHandlingStrategy ExceptionHandling { get; private set; } = ExceptionHandlingStrategy.ThrowOnFirstException;

    public SequentialSignalBroadcastingStrategyConfiguration WithThrowOnFirstException()
    {
        ExceptionHandling = ExceptionHandlingStrategy.ThrowOnFirstException;
        return this;
    }

    public SequentialSignalBroadcastingStrategyConfiguration WithThrowAfterAll()
    {
        ExceptionHandling = ExceptionHandlingStrategy.ThrowAfterAll;
        return this;
    }
}
