namespace Conqueror.Transport.Http.Tests.Signalling.WebSockets;

[TestFixture]
public sealed class HttpWebSocketsSignalExecutionConformityTests
    : SignalTransportExecutionConformityTests<
          HttpWebSocketsSignalExecutionConformityTests,
          HttpSignalTransportConformityTestHost,
          HttpSignalConformityExecutionSuccessTestCase,
          HttpSignalConformityExecutionErrorTestCase>,
      ISignalTransportExecutionConformityTests<
          HttpSignalTransportConformityTestHost,
          HttpSignalConformityExecutionSuccessTestCase,
          HttpSignalConformityExecutionErrorTestCase>
{
    public static bool TransportBuffersSignalsDuringReceiverDowntime => false;

    public static bool TransportSupportsReconnectingReceivers => true;

    public static bool TransportUsesCompetingConsumers => false;

    public static string TransportTypeName => WebSocketsTransportName;

    public static IEnumerable<HttpSignalConformityExecutionSuccessTestCase> CreateSuccessTestCases()
        => HttpSignalTestCases.CreateSuccessTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.WebSockets);

    public static IEnumerable<HttpSignalConformityExecutionSuccessTestCase> CreateSimpleSuccessTestCases()
        => HttpSignalTestCases.CreateSimpleSuccessTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.WebSockets);

    public static IEnumerable<HttpSignalConformityExecutionErrorTestCase> CreateErrorTestCases()
        => HttpSignalTestCases.CreateErrorTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.WebSockets);
}
