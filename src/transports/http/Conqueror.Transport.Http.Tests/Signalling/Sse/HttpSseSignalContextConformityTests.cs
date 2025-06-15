namespace Conqueror.Transport.Http.Tests.Signalling.Sse;

[TestFixture]
public sealed class HttpSseSignalContextConformityTests
    : SignalTransportContextConformityTests<
          HttpSseSignalContextConformityTests,
          HttpSignalTransportConformityTestHost,
          HttpSignalConformityContextTestCase>,
      ISignalTransportContextConformityTests<
          HttpSignalTransportConformityTestHost,
          HttpSignalConformityContextTestCase>
{
    public static IEnumerable<HttpSignalConformityContextTestCase> CreateTestCases()
        => HttpSignalTestCases.CreateContextTestCases(HttpSignalConformityTestCase.HttpSignalTransportType.Sse);
}
