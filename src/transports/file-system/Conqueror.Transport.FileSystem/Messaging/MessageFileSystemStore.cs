namespace Conqueror.Transport.FileSystem.Messaging;

internal sealed class MessageFileSystemStore : IDisposable
{
    private readonly DirectoryPath baseDirectoryPath;
    private readonly TagIdFiles tagIdFiles;

    public MessageFileSystemStore(DirectoryPath baseDirectoryPath)
    {
        this.baseDirectoryPath = baseDirectoryPath;

        tagIdFiles = new(baseDirectoryPath);
        SeqIndexFile = new(baseDirectoryPath, tagIdFiles);
        ContentFiles = new(baseDirectoryPath);
    }

    public SeqIndexFile SeqIndexFile { get; }

    public ContentFiles ContentFiles { get; }

    public InboxFiles GetInboxFiles()
    {
        baseDirectoryPath.AssertExists();

        var directoryPath = baseDirectoryPath.SubDir(".inboxes");

        directoryPath.EnsureExists();

        return new(directoryPath, tagIdFiles);
    }

    public void Dispose()
    {
        SeqIndexFile.Dispose();
    }
}
