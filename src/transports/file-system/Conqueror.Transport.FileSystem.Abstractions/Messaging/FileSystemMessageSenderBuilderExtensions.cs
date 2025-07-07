// ReSharper disable once CheckNamespace
namespace Conqueror;

public static class FileSystemMessageSenderBuilderExtensions
{
    /// <summary>
    ///     Use the provided polling interval to check the file system for the response.<br />
    ///     <br />
    ///     Note that this setting is ignored for messages without a response.
    /// </summary>
    /// <param name="builder">The sender builder</param>
    /// <param name="baseDirectoryPath">The path to the directory to use for sending and receiving messages</param>
    /// <param name="pollingInterval">The interval with which to poll the file system for responses</param>
    /// <returns>The message sender</returns>
    public static IFileSystemMessageSender<TMessage, TResponse> UseFileSystem<TMessage, TResponse>(
        this MessageSenderBuilder<TMessage, TResponse> builder,
        string baseDirectoryPath,
        TimeSpan pollingInterval)
        where TMessage : class, IFileSystemMessage<TMessage, TResponse>
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(baseDirectoryPath);

        if (builder.ServiceProvider.GetService(typeof(IFileSystemMessageSenderFactory)) is not IFileSystemMessageSenderFactory senderFactory)
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IFileSystemMessageSenderFactory)}'; did you forget to add the Conqueror file system services to the service collection?");
        }

        return senderFactory.Create<TMessage, TResponse>(baseDirectoryPath, pollingInterval);
    }
}
