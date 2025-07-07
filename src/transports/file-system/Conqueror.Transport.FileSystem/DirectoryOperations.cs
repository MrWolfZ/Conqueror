namespace Conqueror.Transport.FileSystem;

internal static class DirectoryOperations
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureExists(this DirectoryPath directoryPath)
    {
        _ = Directory.CreateDirectory(directoryPath);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertExists(this DirectoryPath directoryPath)
    {
        Debug.Assert(Directory.Exists(directoryPath), $"expected directory '{directoryPath}' to exist but it does not");
    }
}
