namespace Conqueror.Transport.FileSystem;

internal readonly record struct FilePath
{
    public readonly DirectoryPath DirectoryPath;

    private readonly string path;

    public FilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                $"expected base directory to be non-null, non-whitespace string, but it was {path}",
                nameof(path));
        }

        this.path = path;

        var directoryName = Path.GetDirectoryName(path)
                            ?? throw new ArgumentException($"expected path '{path}' to have a directory name, but it did not");

        DirectoryPath = new(directoryName);
    }

    public static implicit operator string(FilePath path) => path.path;

    public bool Equals(FilePath other) => string.CompareOrdinal(path, other.path) == 0;

    public override int GetHashCode() => path.GetHashCode();

    public override string ToString() => path;
}
