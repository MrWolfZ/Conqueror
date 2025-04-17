using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public abstract class MessageFailedException : Exception
{
    protected MessageFailedException(string? message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected MessageFailedException(string? message)
        : base(message)
    {
    }

    protected MessageFailedException()
    {
    }

    public Type MessageType => MessagePayload.GetType();

    public required object MessagePayload { get; init; }

    public abstract string WellKnownReason { get; }

    public required MessageTransportType TransportType { get; init; }

    [SuppressMessage("Design",
                     "CA1034:Nested types should not be visible",
                     Justification = "This is a conscious design decision to maintain logical coherence")]
    public static class WellKnownReasons
    {
        public const string None = nameof(None);
        public const string Unauthenticated = nameof(Unauthenticated);
        public const string Unauthorized = nameof(Unauthorized);
        public const string InvalidFormattedContextData = nameof(InvalidFormattedContextData);
    }
}
