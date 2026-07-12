namespace Conqueror;

public abstract class IteratorFailedException : Exception
{
    protected IteratorFailedException(string? message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected IteratorFailedException(string? message)
        : base(message)
    {
    }

    protected IteratorFailedException() { }

    public Type IteratorType => IteratorPayload.GetType();

    public required object IteratorPayload { get; init; }

    public abstract string WellKnownReason { get; }

    public required IteratorTransportType TransportType { get; init; }

    [SuppressMessage(
        "Design",
        "CA1034:Nested types should not be visible",
        Justification = "This is a conscious design decision to maintain logical coherence"
    )]
    public static class WellKnownReasons
    {
        public const string None = nameof(None);
        public const string Unauthenticated = nameof(Unauthenticated);
        public const string Unauthorized = nameof(Unauthorized);
        public const string InvalidFormattedContextData = nameof(InvalidFormattedContextData);
    }
}
