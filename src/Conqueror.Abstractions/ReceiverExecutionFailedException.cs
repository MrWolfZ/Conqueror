using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[Serializable]
public sealed class ReceiverExecutionFailedException : Exception
{
    public ReceiverExecutionFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ReceiverExecutionFailedException(string message)
        : base(message)
    {
    }

    private ReceiverExecutionFailedException()
    {
    }

    /// <summary>
    ///     <c>null</c> when the handler is a delegate handler.
    /// </summary>
    public required Type? HandlerType { get; init; }

    public required SignalTransportType SignalTransportType { get; init; }
}
