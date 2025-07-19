// ReSharper disable once CheckNamespace
namespace Conqueror;

public sealed class FileSystemMessageReceiverConfiguration
{
    public required string Name { get; set; }

    public required string BaseDirectoryPath { get; init; }

    public required TimeSpan? LeaseDuration { get; init; }

    public required TimeSpan PollingInterval { get; init; }

    public int? LimitNrOfFailedProcessingAttempts { get; private set; }

    // for now, we'll keep these callback APIs internal for testing and debugging, but we may expose them in the future
    internal Action<object>? MessageCallback { get; private set; }

    internal Action<Exception>? ExceptionCallback { get; private set; }

    /// <summary>
    ///     Set the name to identify receivers which compete for processing the same messages.
    /// </summary>
    /// <param name="name">The name to use</param>
    /// <returns>The configuration</returns>
    public FileSystemMessageReceiverConfiguration WithName(string name)
    {
        Name = name;

        return this;
    }

    public FileSystemMessageReceiverConfiguration WithLimitNrOfFailedProcessingAttempts(int limitNrOfFailedProcessingAttempts)
    {
        LimitNrOfFailedProcessingAttempts = limitNrOfFailedProcessingAttempts;

        return this;
    }

    internal FileSystemMessageReceiverConfiguration WithMessageCallback(Action<object>? signalCallback)
    {
        MessageCallback = signalCallback;

        return this;
    }

    internal FileSystemMessageReceiverConfiguration WithExceptionCallback(Action<Exception>? exceptionCallback)
    {
        ExceptionCallback = exceptionCallback;

        return this;
    }
}
