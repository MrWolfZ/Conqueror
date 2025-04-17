using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public abstract class MessageFailedException<TMessage> : MessageFailedException
    where TMessage : class
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

    public override Type MessageType => typeof(TMessage);

    public required TMessage MessagePayload { get; init; }

    public override object MessagePayloadObject => MessagePayload;
}

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

    public abstract Type MessageType { get; }

    public abstract object MessagePayloadObject { get; }

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
