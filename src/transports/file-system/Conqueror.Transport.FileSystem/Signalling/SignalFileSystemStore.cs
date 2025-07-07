namespace Conqueror.Transport.FileSystem.Signalling;

internal sealed class SignalFileSystemStore(DirectoryPath baseDirectoryPath) : IDisposable
{
    public SeqIndexFile SeqIndexFile { get; } = new(baseDirectoryPath);

    public ContentFiles ContentFiles { get; } = new(baseDirectoryPath);

    public InboxFiles GetInboxFiles()
    {
        baseDirectoryPath.AssertExists();

        var directoryPath = baseDirectoryPath.SubDir(".inboxes");

        directoryPath.EnsureExists();

        return new(directoryPath);
    }

    public void Dispose()
    {
        SeqIndexFile.Dispose();
    }
}
