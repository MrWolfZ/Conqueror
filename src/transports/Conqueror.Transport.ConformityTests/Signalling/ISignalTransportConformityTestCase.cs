namespace Conqueror.Transport.ConformityTests.Signalling;

public interface ISignalTransportConformityTestCase<out TTestHost> : ITransportConformityTestCase
    where TTestHost : ISignalTransportConformityTestHost
{
    TTestHost CreateTestHost();

    Task PublishSignals(ISignalPublishers publishers, CancellationToken cancellationToken);
}

public interface ISignalTransportConformityExecutionTestCase<out TTestHost> : ISignalTransportConformityTestCase<TTestHost>
    where TTestHost : ISignalTransportConformityTestHost
{
    int NumOfReceivers { get; }

    bool SignalsArePublishedInParallel { get; }

    IReadOnlyCollection<object> ExpectedReceivedSignals { get; }
}

public interface ISignalTransportConformityExecutionSuccessTestCase<TTestHost>
    : ISignalTransportConformityExecutionTestCase<TTestHost>
    where TTestHost : ISignalTransportConformityTestHost
{
    bool ShouldCompleteImmediately { get; }

    Task BeforePublish(TTestHost testHost) => Task.CompletedTask;

    Task AfterSignalsAreReceived(TTestHost testHost) => Task.CompletedTask;
}

public interface ISignalTransportConformityExecutionErrorTestCase<TTestHost>
    : ISignalTransportConformityExecutionTestCase<TTestHost>
    where TTestHost : ISignalTransportConformityTestHost
{
    Exception? ReceiverConfigurationException { get; }

    Exception? PublishException { get; }

    IReadOnlyCollection<Exception?> HandlerExceptions { get; }

    int? NumOfExpectedUnrecoverableConnectionErrors { get; }

    Task OnReceiverConfigurationException(TTestHost testHost) => Task.CompletedTask;

    Task OnPublishException(TTestHost testHost) => Task.CompletedTask;

    Task OnInitialReceiverConnection(TTestHost testHost) => Task.CompletedTask;

    Task OnHandlerExceptions(TTestHost testHost) => Task.CompletedTask;

    /// <summary>
    ///     Trigger an error that causes receivers to reconnect.
    /// </summary>
    /// <param name="testHost">the test host</param>
    Task TriggerReconnect(TTestHost testHost) => Task.CompletedTask;

    Task AfterSuccessfulReconnect(TTestHost testHost) => Task.CompletedTask;
}

public interface ISignalTransportConformityContextTestCase<TTestHost> : ISignalTransportConformityTestCase<TTestHost>
    where TTestHost : ISignalTransportConformityTestHost
{
    int NumOfReceivers { get; }

    bool HasActivity { get; }

    bool HasDownstreamData { get; }

    bool HasBidirectionalData { get; }

    Task BeforePublish(TTestHost testHost);
}
