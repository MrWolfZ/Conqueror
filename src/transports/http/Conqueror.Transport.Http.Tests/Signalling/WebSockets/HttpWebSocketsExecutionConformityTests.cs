using Conqueror.Transports.ConformityTests.Signalling;

namespace Conqueror.Transport.Http.Tests.Signalling.WebSockets;

[TestFixture]
public sealed class HttpWebSocketsExecutionConformityTests
    : SignalTransportExecutionConformityTests<
          HttpWebSocketsExecutionConformityTests,
          HttpSignalTransportConformityTestHost,
          HttpSignalConformityExecutionSuccessTestCase,
          HttpSignalConformityExecutionErrorTestCase>,
      ISignalTransportExecutionConformityTests<
          HttpSignalTransportConformityTestHost,
          HttpSignalConformityExecutionSuccessTestCase,
          HttpSignalConformityExecutionErrorTestCase>
{
    public static IEnumerable<HttpSignalConformityExecutionSuccessTestCase> CreateSuccessTestCases()
        => HttpSignalTestCases.CreateSuccessTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.WebSockets);

    public static IEnumerable<HttpSignalConformityExecutionSuccessTestCase> CreateSimpleSuccessTestCases()
        => HttpSignalTestCases.CreateSimpleSuccessTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.WebSockets);

    public static IEnumerable<HttpSignalConformityExecutionErrorTestCase> CreateErrorTestCases()
        => HttpSignalTestCases.CreateErrorTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.WebSockets);

    public static IEnumerable<HttpSignalConformityExecutionErrorTestCase> CreateReconnectDelayTestCases()
        => HttpSignalTestCases.CreateReconnectDelayTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.WebSockets);
}
