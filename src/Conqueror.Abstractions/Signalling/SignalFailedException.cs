namespace Conqueror;

public abstract class SignalFailedException : Exception
{
    protected SignalFailedException(string? message, Exception innerException)
        : base(message, innerException) { }

    protected SignalFailedException(string? message)
        : base(message) { }

    protected SignalFailedException() { }

    public Type SignalType => SignalPayload.GetType();

    public required object SignalPayload { get; init; }

    public abstract string WellKnownReason { get; }

    public required SignalTransportType TransportType { get; init; }

    [SuppressMessage(
        "Design",
        "CA1034:Nested types should not be visible",
        Justification = "This is a conscious design decision to maintain logical coherence"
    )]
    public static class WellKnownReasons
    {
        public const string None = nameof(None);
        public const string InvalidFormattedContextData = nameof(InvalidFormattedContextData);
    }
}
