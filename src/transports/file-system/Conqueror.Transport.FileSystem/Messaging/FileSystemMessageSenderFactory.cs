namespace Conqueror.Transport.FileSystem.Messaging;

internal sealed class FileSystemMessageSenderFactory(FileSystemStores fileSystemStores) : IFileSystemMessageSenderFactory
{
    public IFileSystemMessageSender<TMessage, TResponse> Create<TMessage, TResponse>(string baseDirectoryPath, TimeSpan pollingInterval)
        where TMessage : class, IFileSystemMessage<TMessage, TResponse>
    {
        var store = fileSystemStores.GetMessageStore(new(baseDirectoryPath));

        return new FileSystemMessageSender<TMessage, TResponse>(store, pollingInterval);
    }
}
