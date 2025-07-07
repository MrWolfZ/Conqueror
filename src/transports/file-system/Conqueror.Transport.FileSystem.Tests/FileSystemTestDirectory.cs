namespace Conqueror.Transport.FileSystem.Tests;

internal static class FileSystemTestDirectory
{
    public static DirectoryInfo Create()
    {
        var testCaseDataDir = Path.Join(TestContext.CurrentContext.TestDirectory, ".test-data", Guid.NewGuid().ToString());
        var dirInfo = new DirectoryInfo(testCaseDataDir);

        dirInfo.Create();

        return dirInfo;
    }

    public static void CleanAll()
    {
        var testDataDir = Path.Join(TestContext.CurrentContext.TestDirectory, ".test-data");

        if (Directory.Exists(testDataDir))
        {
            Directory.Delete(testDataDir, recursive: true);
        }
    }
}
