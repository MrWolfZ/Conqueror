using Conqueror.Transport.ConformityTests.Messaging;

namespace Conqueror.Transport.Http.Tests.Messaging;

public sealed class HttpMessageConformityContextTestCase : HttpMessageConformityTestCase,
                                                           IMessageTransportConformityContextTestCase<HttpMessageTransportConformityTestHost>
{
    public required bool HasActivity { get; init; }

    public required bool HasDownstreamData { get; init; }

    public required bool HasBidirectionalData { get; init; }

    public required bool HasUpstreamData { get; init; }
}
