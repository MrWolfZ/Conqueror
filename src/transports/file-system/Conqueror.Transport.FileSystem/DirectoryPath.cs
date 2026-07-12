namespace Conqueror.Transport.FileSystem;

using static Path;

internal readonly record struct DirectoryPath
{
    private readonly string path;

    public DirectoryPath(string path)
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

    public static implicit operator string(DirectoryPath dir) => dir.path;

    public bool Equals(DirectoryPath other) => string.CompareOrdinal(path, other.path) is 0;

    public DirectoryPath SubDir(string pathSegment) =>
        new(
            new(
                path.TrimEnd(DirectorySeparatorChar)
                + DirectorySeparatorChar
                + pathSegment.TrimStart(DirectorySeparatorChar)
            )
        );

    public FilePath File(string fileName) =>
        new(
            new(
                path.TrimEnd(DirectorySeparatorChar)
                + DirectorySeparatorChar
                + fileName.TrimStart(DirectorySeparatorChar)
            )
        );

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(path);

    public override string ToString() => path;
}
