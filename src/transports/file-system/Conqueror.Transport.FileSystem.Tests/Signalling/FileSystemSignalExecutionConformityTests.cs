namespace Conqueror.Transport.FileSystem.Tests.Signalling;

[TestFixture]
public sealed class FileSystemSignalExecutionConformityTests
    : SignalTransportExecutionConformityTests<
          FileSystemSignalExecutionConformityTests,
          FileSystemSignalTransportConformityTestHost,
          FileSystemSignalConformityExecutionSuccessTestCase,
          FileSystemSignalConformityExecutionErrorTestCase
      >,
      ISignalTransportExecutionConformityTests<
          FileSystemSignalTransportConformityTestHost,
          FileSystemSignalConformityExecutionSuccessTestCase,
          FileSystemSignalConformityExecutionErrorTestCase
      >
{
    public static bool TransportBuffersSignalsDuringReceiverDowntime => true;

    public static bool TransportSupportsReconnectingReceivers => false;

    public static bool TransportUsesCompetingConsumers => true;

    public static string TransportTypeName => TransportName;

    public static IEnumerable<FileSystemSignalConformityExecutionSuccessTestCase> CreateSuccessTestCases() =>
        FileSystemSignalTestCases.CreateSuccessTestCases();

    public static IEnumerable<FileSystemSignalConformityExecutionSuccessTestCase> CreateSimpleSuccessTestCases() =>
        FileSystemSignalTestCases.CreateSimpleSuccessTestCases();

    public static IEnumerable<FileSystemSignalConformityExecutionErrorTestCase> CreateErrorTestCases() =>
        FileSystemSignalTestCases.CreateErrorTestCases();
}
