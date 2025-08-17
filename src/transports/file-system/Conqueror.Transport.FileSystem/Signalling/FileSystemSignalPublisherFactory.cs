namespace Conqueror.Transport.FileSystem.Signalling;

internal sealed class FileSystemSignalPublisherFactory(FileSystemStores fileSystemStores)
    : IFileSystemSignalPublisherFactory
{
    public IFileSystemSignalPublisher<TSignal> Get<TSignal>(string baseDirectoryPath)
        where TSignal : class, IFileSystemSignal<TSignal>
    {
        var store = fileSystemStores.GetSignalStore(new(baseDirectoryPath));

        return new FileSystemSignalPublisher<TSignal>(store);
    }
}
