namespace Conqueror;

public sealed class IteratorServerExecutionFailedException : Exception
{
    public IteratorServerExecutionFailedException(string message, Exception innerException)
        : base(message, innerException) { }

    public IteratorServerExecutionFailedException(string message)
        : base(message) { }

    private IteratorServerExecutionFailedException() { }

    /// <summary>
    ///     <see langword="null" /> when the handler is a delegate handler.
    /// </summary>
    public required Type? HandlerType { get; init; }

    public required IteratorTransportType IteratorTransportType { get; init; }
}
