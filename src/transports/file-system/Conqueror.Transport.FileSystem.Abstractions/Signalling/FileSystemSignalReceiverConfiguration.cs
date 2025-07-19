// ReSharper disable once CheckNamespace
namespace Conqueror;

public sealed class FileSystemSignalReceiverConfiguration
{
    public required string Name { get; set; }

    public required string BaseDirectoryPath { get; init; }

    public required TimeSpan LeaseDuration { get; init; }

    public required TimeSpan PollingInterval { get; init; }

    // for now, we'll keep these callback APIs internal for testing and debugging, but we may expose them in the future
    internal Action<object>? SignalCallback { get; private set; }

    internal Action<Exception>? ExceptionCallback { get; private set; }

    /// <summary>
    ///     Set the name to identify receivers which compete for processing the same signals.
    /// </summary>
    /// <param name="name">The name to use</param>
    /// <returns>The configuration</returns>
    public FileSystemSignalReceiverConfiguration WithName(string name)
    {
        Name = name;

        return this;
    }

    internal FileSystemSignalReceiverConfiguration WithSignalCallback(Action<object>? signalCallback)
    {
        SignalCallback = signalCallback;

        return this;
    }

    internal FileSystemSignalReceiverConfiguration WithExceptionCallback(Action<Exception>? exceptionCallback)
    {
        ExceptionCallback = exceptionCallback;

        return this;
    }
}
