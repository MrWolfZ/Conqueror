namespace Conqueror;

public sealed class MessageReceiverExecutionFailedException : Exception
{
    public MessageReceiverExecutionFailedException(string message, Exception innerException)
        : base(message, innerException) { }

    public MessageReceiverExecutionFailedException(string message)
        : base(message) { }

    private MessageReceiverExecutionFailedException() { }

    /// <summary>
    ///     <see langword="null" /> when the handler is a delegate handler.
    /// </summary>
    public required Type? HandlerType { get; init; }

    public required MessageTransportType MessageTransportType { get; init; }
}
