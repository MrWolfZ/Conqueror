namespace Conqueror.Transport.Http.Tests.Signalling;

public sealed class HttpSignalConformityContextTestCase : HttpSignalConformityTestCase,
                                                          ISignalTransportConformityContextTestCase<HttpSignalTransportConformityTestHost>
{
    public HttpSignalConformityContextTestCase()
    {
        BeforePublish = host =>
        {
            Assert.That(host.PublisherHost.ServerResponseHasBegunCount, Is.EqualTo(NumOfReceivers * host.ReceiverHosts.Count));

            host.PublisherHost.ServerResponseHasBegunCount = 0;

            return Task.CompletedTask;
        };
    }

    public int NumOfReceivers { get; init; } = 1;

    public required bool HasActivity { get; init; }

    public required bool HasDownstreamData { get; init; }

    public required bool HasBidirectionalData { get; init; }

    public Func<HttpSignalTransportConformityTestHost, Task> BeforePublish { get; init; }

    Task ISignalTransportConformityContextTestCase<HttpSignalTransportConformityTestHost>.BeforePublish(
        HttpSignalTransportConformityTestHost testHost)
        => BeforePublish(testHost);
}
