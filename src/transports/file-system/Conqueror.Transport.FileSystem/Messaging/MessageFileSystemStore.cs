namespace Conqueror.Transport.FileSystem.Messaging;

internal sealed class MessageFileSystemStore(DirectoryPath baseDirectoryPath) : IDisposable
{
    public SeqIndexFile SeqIndexFile { get; } = new(baseDirectoryPath);

    public InboxFiles InboxFiles { get; } = new(baseDirectoryPath);

    public ContentFiles ContentFiles { get; } = new(baseDirectoryPath);

    public void Dispose()
    {
        SeqIndexFile.Dispose();
        InboxFiles.Dispose();
    }
}
