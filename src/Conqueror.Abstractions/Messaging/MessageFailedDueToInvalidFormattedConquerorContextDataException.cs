using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

/// <summary>
///     An exception that represents badly formatted Conqueror context data.
/// </summary>
public sealed class MessageFailedDueToInvalidFormattedConquerorContextDataException : MessageFailedException
{
    public MessageFailedDueToInvalidFormattedConquerorContextDataException(string? message, Exception innerException)
        : base(message, innerException)
    {
    }

    public MessageFailedDueToInvalidFormattedConquerorContextDataException(string? message)
        : base(message)
    {
    }

    public MessageFailedDueToInvalidFormattedConquerorContextDataException()
    {
    }

    public override string WellKnownReason => WellKnownReasons.InvalidFormattedContextData;
}
