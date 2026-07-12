namespace Conqueror.Transport.FileSystem.Tests.Messaging;

[TestFixture]
public sealed class FileSystemMessageContextConformityTests
    : MessageTransportContextConformityTests<
          FileSystemMessageContextConformityTests,
          FileSystemMessageTransportConformityTestHost,
          FileSystemMessageConformityContextTestCase
      >,
      IMessageTransportContextConformityTests<
          FileSystemMessageTransportConformityTestHost,
          FileSystemMessageConformityContextTestCase
      >
{
    public static IEnumerable<FileSystemMessageConformityContextTestCase> CreateTestCases() =>
        FileSystemMessageTestCases.CreateContextTestCases();
}
