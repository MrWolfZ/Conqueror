using Conqueror.Transports.ConformityTests.Signalling;

namespace Conqueror.Transport.Http.Tests.Signalling;

public sealed class HttpSignalConformityExecutionSuccessTestCase : HttpSignalConformityExecutionTestCase,
                                                          ISignalTransportConformityExecutionSuccessTestCase<HttpSignalTransportConformityTestHost>
{
    public HttpSignalConformityExecutionSuccessTestCase()
    {
        OnConnectionSuccess = (host, numOfRuns) =>
        {
            Assert.That(host.ServerResponseHasBegunCount, Is.EqualTo(NumOfReceivers * numOfRuns));

            host.ServerResponseHasBegunCount = 0;

            return Task.CompletedTask;
        };
    }

    public bool ShouldCompleteImmediately { get; init; }

    public Func<HttpSignalTransportConformityTestHost, int, Task> OnConnectionSuccess { get; init; }

    public Func<HttpSignalTransportConformityTestHost, Task>? OnReceiveSuccess { get; init; }

    Task ISignalTransportConformityExecutionSuccessTestCase<HttpSignalTransportConformityTestHost>.OnConnectionSuccess(
        HttpSignalTransportConformityTestHost testHost,
        int numOfRuns)
        => OnConnectionSuccess(testHost, numOfRuns);

    Task ISignalTransportConformityExecutionSuccessTestCase<HttpSignalTransportConformityTestHost>.OnReceiveSuccess(HttpSignalTransportConformityTestHost testHost)
        => OnReceiveSuccess?.Invoke(testHost) ?? Task.CompletedTask;
}
