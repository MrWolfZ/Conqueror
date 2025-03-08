using System;

namespace Conqueror;

/// <summary>
///     This exception is thrown when trying to publish an event which is annotated with one
///     or more custom transport attributes, but no publisher is registered for any of those
///     attributes.
/// </summary>
[Serializable]
public sealed class UnregisteredEventTransportPublisherException : Exception
{
    public UnregisteredEventTransportPublisherException(string message)
        : base(message)
    {
    }

    public UnregisteredEventTransportPublisherException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public UnregisteredEventTransportPublisherException()
    {
    }
}
