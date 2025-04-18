namespace Conqueror.Eventing;

public sealed record SequentialEventNotificationBroadcastingStrategyConfiguration
{
    public ExceptionHandlingStrategy ExceptionHandling { get; private set; } = ExceptionHandlingStrategy.ThrowOnFirstException;

    public SequentialEventNotificationBroadcastingStrategyConfiguration WithThrowOnFirstException()
    {
        ExceptionHandling = ExceptionHandlingStrategy.ThrowOnFirstException;
        return this;
    }

    public SequentialEventNotificationBroadcastingStrategyConfiguration WithThrowAfterAll()
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
