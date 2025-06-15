using Conqueror.Transport.ConformityTests.Messaging;

namespace Conqueror.Transport.Http.Tests.Messaging;

[TestFixture]
public sealed class HttpMessageContextConformityTests
    : MessageTransportContextConformityTests<
          HttpMessageContextConformityTests,
          HttpMessageTransportConformityTestHost,
          HttpMessageConformityContextTestCase>,
      IMessageTransportContextConformityTests<
          HttpMessageTransportConformityTestHost,
          HttpMessageConformityContextTestCase>
{
    public static IEnumerable<HttpMessageConformityContextTestCase> CreateTestCases()
        => HttpMessageTestCases.CreateContextTestCases();
}
