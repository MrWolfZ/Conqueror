namespace Conqueror.Transport.FileSystem;

internal readonly record struct FilePath
{
    private readonly string path;

    public FilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                $"expected base directory to be non-null, non-whitespace string, but it was {path}",
                nameof(path)
            );
        }

        this.path = path;
    }

    public DirectoryPath DirectoryPath =>
        new(
            Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException(
                $"expected path '{path}' to have a directory name, but it did not"
            )
        );

    public static implicit operator string(FilePath path) => path.path;

    public bool Equals(FilePath other) => string.CompareOrdinal(path, other.path) is 0;

    public override int GetHashCode() => path.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => path;
}
