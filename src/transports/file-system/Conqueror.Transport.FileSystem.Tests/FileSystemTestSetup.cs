namespace Conqueror.Transport.FileSystem.Tests;

[SetUpFixture]
internal static class FileSystemTestSetup
{
    [OneTimeSetUp]
    public static void RunBeforeAnyTests()
    {
        FileSystemTestDirectory.CleanAll();
    }

    [OneTimeTearDown]
    public static void RunAfterAnyTests()
    {
        FileSystemTestDirectory.CleanAll();
    }
}
