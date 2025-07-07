namespace Conqueror.Transport.FileSystem.Tests.Signalling;

[TestFixture]
public sealed class FileSystemSignalContextConformityTests
    : SignalTransportContextConformityTests<
          FileSystemSignalContextConformityTests,
          FileSystemSignalTransportConformityTestHost,
          FileSystemSignalConformityContextTestCase>,
      ISignalTransportContextConformityTests<
          FileSystemSignalTransportConformityTestHost,
          FileSystemSignalConformityContextTestCase>
{
    public static IEnumerable<FileSystemSignalConformityContextTestCase> CreateTestCases()
        => FileSystemSignalTestCases.CreateContextTestCases();
}
