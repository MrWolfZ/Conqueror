using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IFileSystemMessageReceiver
{
    IReadOnlyCollection<Type> MessageTypes { get; }

    Type? HandlerType { get; }

    /// <summary>
    ///     Note that this is the service provider from the global scope.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    bool IsEnabled { get; }

    FileSystemMessageReceiverConfiguration? Configuration { get; }

    /// <summary>
    ///     Enable this receiver as the only instance for the message type(s). Messages are guaranteed to be
    ///     processed in the order in which they are sent.
    /// </summary>
    /// <param name="baseDirectoryPath">The path to the directory to use for sending and receiving messages</param>
    /// <param name="pollingInterval">The interval with which to poll the file system for new messages</param>
    /// <returns>The receiver's configuration</returns>
    FileSystemMessageReceiverConfiguration EnableSingleInstance(string baseDirectoryPath, TimeSpan pollingInterval);

    /// <summary>
    ///     Enable this receiver as part of a set of competing instances. Messages will be leased by an instance
    ///     until they are successfully processed, processing fails, or the lease expires (in the latter two cases
    ///     the message becomes available for processing by other instances).
    /// </summary>
    /// <param name="baseDirectoryPath">The path to the directory to use for sending and receiving messages</param>
    /// <param name="leaseDuration">The duration a handler has to process the message before it becomes available to other handlers</param>
    /// <param name="pollingInterval">The interval with which to poll the file system for new messages</param>
    /// <returns>The receiver's configuration</returns>
    FileSystemMessageReceiverConfiguration EnableMultipleCompetingInstances(string baseDirectoryPath, TimeSpan leaseDuration, TimeSpan pollingInterval);

    void Disable();
}
