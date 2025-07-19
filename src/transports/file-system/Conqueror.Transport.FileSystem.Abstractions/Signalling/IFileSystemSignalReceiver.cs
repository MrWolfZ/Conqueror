using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IFileSystemSignalReceiver
{
    IReadOnlyCollection<Type> SignalTypes { get; }

    Type? HandlerType { get; }

    /// <summary>
    ///     Note that this is (usually) the service provider from the global scope,
    ///     and <i>not</i> the service provider from the scope of the send operation.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    bool IsEnabled { get; }

    FileSystemSignalReceiverConfiguration? Configuration { get; }

    /// <summary>
    ///     Enable this receiver as the only instance for the name. Signals are guaranteed to be
    ///     processed in the order in which they are published.
    /// </summary>
    /// <param name="baseDirectoryPath">The path to the directory to use for publishing and receiving signals</param>
    /// <param name="pollingInterval">The interval with which to poll the file system for new signals</param>
    /// <returns>The receiver's configuration</returns>
    FileSystemSignalReceiverConfiguration EnableSingleInstance(string baseDirectoryPath, TimeSpan pollingInterval);

    /// <summary>
    ///     Enable this receiver as part of a set of competing instances. Signals will be leased by an instance
    ///     until they are successfully processed, processing fails, or the lease expires (in the latter two cases
    ///     the signal becomes available for processing by other instances with the same name).
    /// </summary>
    /// <param name="baseDirectoryPath">The path to the directory to use for publishing and receiving signals</param>
    /// <param name="leaseDuration">The duration a handler has to process the signal before it becomes available to other handlers</param>
    /// <param name="pollingInterval">The interval with which to poll the file system for new signals</param>
    /// <returns>The receiver's configuration</returns>
    FileSystemSignalReceiverConfiguration EnableMultipleCompetingInstances(
        string baseDirectoryPath,
        TimeSpan leaseDuration,
        TimeSpan pollingInterval);

    void Disable();
}
