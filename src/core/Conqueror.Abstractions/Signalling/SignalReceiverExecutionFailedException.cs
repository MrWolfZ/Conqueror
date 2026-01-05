namespace Conqueror;

public sealed class SignalReceiverExecutionFailedException : Exception
{
    public SignalReceiverExecutionFailedException(string message, Exception innerException)
        : base(message, innerException) { }

    public SignalReceiverExecutionFailedException(string message)
        : base(message) { }

    private SignalReceiverExecutionFailedException() { }

    /// <summary>
    ///     <see langword="null" /> when the handler is a delegate handler.
    /// </summary>
    public required Type? HandlerType { get; init; }

    public required SignalTransportType SignalTransportType { get; init; }
}
