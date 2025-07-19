using Conqueror.Transport.FileSystem.Messaging;
using Conqueror.Transport.FileSystem.Signalling;

namespace Conqueror.Transport.FileSystem;

internal sealed class FileSystemStores : IDisposable
{
    private readonly ConcurrentDictionary<DirectoryPath, MessageFileSystemStore> messageStoreByBaseDirectory = [];
    private readonly ConcurrentDictionary<DirectoryPath, SignalFileSystemStore> signalStoreByBaseDirectory = [];

    private int disposedFlag;

    public MessageFileSystemStore GetMessageStore(DirectoryPath baseDirectoryPath)
    {
        ThrowIfDisposed();

        baseDirectoryPath.AssertExists();

        var messageStoreDirectory = baseDirectoryPath.SubDir("messages");

        messageStoreDirectory.EnsureExists();

        if (!messageStoreByBaseDirectory.TryGetValue(baseDirectoryPath, out var store))
        {
            var newStore = new MessageFileSystemStore(messageStoreDirectory);

            if (!messageStoreByBaseDirectory.TryAdd(baseDirectoryPath, newStore))
            {
                newStore.Dispose();
            }

            store = messageStoreByBaseDirectory[baseDirectoryPath];
        }

        return store;
    }

    public SignalFileSystemStore GetSignalStore(DirectoryPath baseDirectoryPath)
    {
        ThrowIfDisposed();

        baseDirectoryPath.AssertExists();

        var signalStoreDirectory = baseDirectoryPath.SubDir("signals");

        signalStoreDirectory.EnsureExists();

        if (!signalStoreByBaseDirectory.TryGetValue(baseDirectoryPath, out var store))
        {
            var newStore = new SignalFileSystemStore(signalStoreDirectory);

            if (!signalStoreByBaseDirectory.TryAdd(baseDirectoryPath, newStore))
            {
                newStore.Dispose();
            }

            store = signalStoreByBaseDirectory[baseDirectoryPath];
        }

        return store;
    }

    public void Dispose()
    {
        var prevValue = Interlocked.Exchange(ref disposedFlag, 1);

        if (prevValue != 0)
        {
            return;
        }

        foreach (var store in messageStoreByBaseDirectory.Values)
        {
            store.Dispose();
        }

        foreach (var store in signalStoreByBaseDirectory.Values)
        {
            store.Dispose();
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposedFlag != 0, typeof(FileSystemStores));
}
