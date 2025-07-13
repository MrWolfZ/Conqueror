using Conqueror.Transport.ConformityTests.Messaging;

namespace Conqueror.Transport.Http.Tests.Messaging;

[TestFixture]
public sealed class HttpMessageExecutionConformityTests
    : MessageTransportExecutionConformityTests<
          HttpMessageExecutionConformityTests,
          HttpMessageTransportConformityTestHost,
          HttpMessageConformityExecutionSuccessTestCase,
          HttpMessageConformityExecutionErrorTestCase>,
      IMessageTransportExecutionConformityTests<
          HttpMessageTransportConformityTestHost,
          HttpMessageConformityExecutionSuccessTestCase,
          HttpMessageConformityExecutionErrorTestCase>
{
    public static bool TransportSupportsConcurrentReceivers => false;

    public static bool TransportBuffersMessagesDuringReceiverDowntime => false;

    public static bool TransportRequiresReceiverConnection => false;

    public static bool TransportSupportsReconnectingReceivers => false;

    public static string TransportTypeName => TransportName;

    public static IEnumerable<HttpMessageConformityExecutionSuccessTestCase> CreateSuccessTestCases()
        => HttpMessageTestCases.CreateSuccessTestCases();

    public static IEnumerable<HttpMessageConformityExecutionSuccessTestCase> CreateSimpleSuccessTestCases()
        => HttpMessageTestCases.CreateSimpleSuccessTestCases();

    public static IEnumerable<HttpMessageConformityExecutionErrorTestCase> CreateErrorTestCases()
        => HttpMessageTestCases.CreateErrorTestCases();
}
