using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[Serializable]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "the standard constructors don't make sense for this class")]
public class MessageFailedException : Exception
{
    public MessageFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public MessageFailedException(string message)
        : base(message)
    {
    }

    protected MessageFailedException()
    {
    }

    public required Type MessageType { get; init; }

    public required string Reason { get; init; }

    public required MessageTransportType TransportType { get; init; }

    [SuppressMessage("Design",
                     "CA1034:Nested types should not be visible",
                     Justification = "This is a conscious design decision to maintain logical coherence")]
    public static class WellKnownReasons
    {
        public const string UnauthenticatedPrincipal = nameof(UnauthenticatedPrincipal);
        public const string UnauthorizedPrincipal = nameof(UnauthorizedPrincipal);
        public const string InvalidFormattedContextData = nameof(InvalidFormattedContextData);
    }
}
