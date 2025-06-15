namespace Conqueror.Transport.Http.Tests.Signalling;

public sealed class HttpSignalConformityExecutionSuccessTestCase : HttpSignalConformityExecutionTestCase,
                                                                   ISignalTransportConformityExecutionSuccessTestCase<HttpSignalTransportConformityTestHost>
{
    public HttpSignalConformityExecutionSuccessTestCase()
    {
        BeforePublish = host =>
        {
            Assert.That(host.PublisherHost.ServerResponseHasBegunCount, Is.EqualTo(NumOfReceivers * host.ReceiverHosts.Count));

            host.PublisherHost.ServerResponseHasBegunCount = 0;

            return Task.CompletedTask;
        };
    }

    public bool ShouldCompleteImmediately { get; init; }

    public Func<HttpSignalTransportConformityTestHost, Task> BeforePublish { get; init; }

    public Func<HttpSignalTransportConformityTestHost, Task>? AfterSignalsAreReceived { get; init; }

    Task ISignalTransportConformityExecutionSuccessTestCase<HttpSignalTransportConformityTestHost>.BeforePublish(
        HttpSignalTransportConformityTestHost testHost)
        => BeforePublish(testHost);

    Task ISignalTransportConformityExecutionSuccessTestCase<HttpSignalTransportConformityTestHost>.AfterSignalsAreReceived(
        HttpSignalTransportConformityTestHost testHost)
        => AfterSignalsAreReceived?.Invoke(testHost) ?? Task.CompletedTask;
}
