namespace Conqueror.Transport.FileSystem.Tests.Messaging;

[TestFixture]
public sealed class FileSystemMessageExecutionConformityTests
    : MessageTransportExecutionConformityTests<
        FileSystemMessageExecutionConformityTests,
        FileSystemMessageTransportConformityTestHost,
        FileSystemMessageConformityExecutionSuccessTestCase,
        FileSystemMessageConformityExecutionErrorTestCase
    >,
        IMessageTransportExecutionConformityTests<
            FileSystemMessageTransportConformityTestHost,
            FileSystemMessageConformityExecutionSuccessTestCase,
            FileSystemMessageConformityExecutionErrorTestCase
        >
{
    public static bool TransportSupportsConcurrentReceivers => true;

    public static bool TransportBuffersMessagesDuringReceiverDowntime => true;

    public static bool TransportRequiresReceiverConnection => false;

    public static string TransportTypeName => TransportName;

    public static IEnumerable<FileSystemMessageConformityExecutionSuccessTestCase> CreateSuccessTestCases() =>
        FileSystemMessageTestCases.CreateSuccessTestCases();

    public static IEnumerable<FileSystemMessageConformityExecutionSuccessTestCase> CreateSimpleSuccessTestCases() =>
        FileSystemMessageTestCases.CreateSimpleSuccessTestCases();

    public static IEnumerable<FileSystemMessageConformityExecutionErrorTestCase> CreateErrorTestCases() =>
        FileSystemMessageTestCases.CreateErrorTestCases();
}
