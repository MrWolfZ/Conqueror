namespace Conqueror.Transport.Http.Tests.Signalling.Sse;

[TestFixture]
public sealed class HttpSseContextConformityTests
    : SignalTransportContextConformityTests<
          HttpSseContextConformityTests,
          HttpSignalTransportConformityTestHost,
          HttpSignalConformityContextTestCase>,
      ISignalTransportContextConformityTests<
          HttpSignalTransportConformityTestHost,
          HttpSignalConformityContextTestCase>
{
    public static IEnumerable<HttpSignalConformityContextTestCase> CreateTestCases()
        => HttpSignalTestCases.CreateContextTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.Sse);
}
