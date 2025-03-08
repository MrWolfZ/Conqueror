using System;

namespace Conqueror;

/// <summary>
///     This exception is thrown when trying to publish an event which is annotated with one
///     or more custom transport attributes, but no publisher is registered for any of those
///     attributes.
/// </summary>
[Serializable]
public sealed class ConquerorUnknownEventTransportPublisherException : Exception
{
    public ConquerorUnknownEventTransportPublisherException(string message)
        : base(message)
    {
    }

    public ConquerorUnknownEventTransportPublisherException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ConquerorUnknownEventTransportPublisherException()
    {
    }
}
