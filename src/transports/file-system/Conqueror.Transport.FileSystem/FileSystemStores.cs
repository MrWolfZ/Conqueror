using Conqueror.Transport.FileSystem.Messaging;
using Conqueror.Transport.FileSystem.Signalling;

namespace Conqueror.Transport.FileSystem;

internal sealed class FileSystemStores : IDisposable
{
    private readonly ConcurrentDictionary<DirectoryPath, MessageFileSystemStore> messageStoreByBaseDirectory = [];
    private readonly ConcurrentDictionary<DirectoryPath, SignalFileSystemStore> signalStoreByBaseDirectory = [];

    public MessageFileSystemStore GetMessageStore(DirectoryPath baseDirectoryPath)
    {
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
        foreach (var file in messageStoreByBaseDirectory.Values)
        {
            file.Dispose();
        }
    }
}
