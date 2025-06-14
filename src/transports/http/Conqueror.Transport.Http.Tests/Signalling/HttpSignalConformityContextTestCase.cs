namespace Conqueror.Transport.Http.Tests.Signalling;

public sealed class HttpSignalConformityContextTestCase : HttpSignalConformityTestCase,
                                                          ISignalTransportConformityContextTestCase<HttpSignalTransportConformityTestHost>
{
    public HttpSignalConformityContextTestCase()
    {
        OnConnectionSuccess = (host, numOfRuns) =>
        {
            Assert.That(host.ServerResponseHasBegunCount, Is.EqualTo(NumOfReceivers * numOfRuns));

            host.ServerResponseHasBegunCount = 0;

            return Task.CompletedTask;
        };
    }

    public int NumOfReceivers { get; init; } = 1;

    public required bool HasActivity { get; init; }

    public required bool HasDownstreamData { get; init; }

    public required bool HasBidirectionalData { get; init; }

    public Func<HttpSignalTransportConformityTestHost, int, Task> OnConnectionSuccess { get; init; }

    Task ISignalTransportConformityContextTestCase<HttpSignalTransportConformityTestHost>.OnConnectionSuccess(
        HttpSignalTransportConformityTestHost testHost,
        int numOfRuns)
        => OnConnectionSuccess(testHost, numOfRuns);
}
