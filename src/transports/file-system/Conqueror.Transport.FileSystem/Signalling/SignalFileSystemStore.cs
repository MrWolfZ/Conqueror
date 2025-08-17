namespace Conqueror.Transport.FileSystem.Signalling;

internal sealed class SignalFileSystemStore : IDisposable
{
    private readonly DirectoryPath baseDirectoryPath;
    private readonly TagIdFiles tagIdFiles;

    public SignalFileSystemStore(DirectoryPath baseDirectoryPath)
    {
        this.baseDirectoryPath = baseDirectoryPath;

        tagIdFiles = new TagIdFiles(baseDirectoryPath);
        SeqIndexFile = new SeqIndexFile(baseDirectoryPath, tagIdFiles);
        ContentFiles = new ContentFiles(baseDirectoryPath);
    }

    public SeqIndexFile SeqIndexFile { get; }

    public ContentFiles ContentFiles { get; }

    public void Dispose() => SeqIndexFile.Dispose();

    public InboxFiles GetInboxFiles()
    {
        baseDirectoryPath.AssertExists();

        var directoryPath = baseDirectoryPath.SubDir(".inboxes");

        directoryPath.EnsureExists();

        return new InboxFiles(directoryPath, tagIdFiles);
    }
}
