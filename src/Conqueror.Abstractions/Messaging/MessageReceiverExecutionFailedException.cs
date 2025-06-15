using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[Serializable]
public sealed class MessageReceiverExecutionFailedException : Exception
{
    public MessageReceiverExecutionFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public MessageReceiverExecutionFailedException(string message)
        : base(message)
    {
    }

    private MessageReceiverExecutionFailedException()
    {
    }

    /// <summary>
    ///     <c>null</c> when the handler is a delegate handler.
    /// </summary>
    public required Type? HandlerType { get; init; }

    public required MessageTransportType MessageTransportType { get; init; }
}
