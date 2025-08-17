namespace Conqueror.Transport.Http.Tests.Signalling.WebSockets;

[TestFixture]
public sealed class HttpWebSocketsSignalContextConformityTests
    : SignalTransportContextConformityTests<
          HttpWebSocketsSignalContextConformityTests,
          HttpSignalTransportConformityTestHost,
          HttpSignalConformityContextTestCase
      >,
      ISignalTransportContextConformityTests<
          HttpSignalTransportConformityTestHost,
          HttpSignalConformityContextTestCase
      >
{
    public static IEnumerable<HttpSignalConformityContextTestCase> CreateTestCases() =>
        HttpSignalTestCases.CreateContextTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.WebSockets);
}
