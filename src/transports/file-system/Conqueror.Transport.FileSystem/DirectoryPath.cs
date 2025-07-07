using static System.IO.Path;

namespace Conqueror.Transport.FileSystem;

internal readonly record struct DirectoryPath
{
    private readonly string path;

    public DirectoryPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                $"expected base directory to be non-null, non-whitespace string, but it was {path}",
                nameof(path));
        }

        this.path = path;
    }

    public static implicit operator string(DirectoryPath dir) => dir.path;

    public DirectoryPath SubDir(string pathSegment)
        => new(
            new(path.TrimEnd(DirectorySeparatorChar) + DirectorySeparatorChar + pathSegment.TrimStart(DirectorySeparatorChar)));

    public FilePath File(string fileName)
        => new(
            new(path.TrimEnd(DirectorySeparatorChar) + DirectorySeparatorChar + fileName.TrimStart(DirectorySeparatorChar)));

    public bool Equals(DirectoryPath other) => string.CompareOrdinal(path, other.path) == 0;

    public override int GetHashCode() => path.GetHashCode();

    public override string ToString() => path;
}
