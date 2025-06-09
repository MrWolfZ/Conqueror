using Conqueror.Transports.ConformityTests.Signalling;

namespace Conqueror.Transport.Http.Tests.Signalling.WebSockets;

[TestFixture]
public sealed class HttpWebSocketsContextConformityTests
    : SignalTransportContextConformityTests<
          HttpWebSocketsContextConformityTests,
          HttpSignalTransportConformityTestHost,
          HttpSignalConformityContextTestCase>,
      ISignalTransportContextConformityTests<
          HttpSignalTransportConformityTestHost,
          HttpSignalConformityContextTestCase>
{
    public static IEnumerable<HttpSignalConformityContextTestCase> CreateTestCases()
        => HttpSignalTestCases.CreateContextTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.WebSockets);
}
