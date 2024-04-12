using System;

namespace Conqueror;

/// <summary>
///     This exception is thrown when trying to publish an event which is annotated with a
///     custom <see cref="IConquerorEventTransportConfigurationAttribute " />, but no publisher is
///     registered for that attribute.
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
