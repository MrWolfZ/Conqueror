namespace Conqueror.Transports.ConformityTests.Signalling;

public interface ISignalTransportConformityTestCase<TTestHost> : ITransportConformityTestCase
    where TTestHost : ISignalTransportConformityTestHost<TTestHost>
{
    Task<TTestHost> CreateTestHost();

    SignalReceiverExecutionHandle RunReceivers(
        ISignalReceivers receivers,
        CancellationToken cancellationToken,
        Func<object, ConquerorContext, CancellationToken, Task>? signalCallback = null,
        Func<CancellationToken, Task>? reconnectDelayCallback = null);

    Task PublishSignals(
        ISignalPublishers publishers,
        CancellationToken cancellationToken,
        Func<object, ConquerorContext, CancellationToken, Task>? publishCallback = null);
}

public interface ISignalTransportConformityExecutionTestCase<TTestHost> : ISignalTransportConformityTestCase<TTestHost>
    where TTestHost : ISignalTransportConformityTestHost<TTestHost>
{
    int NumOfReceivers { get; }

    IReadOnlyCollection<object> ExpectedReceivedSignals { get; }
}

public interface ISignalTransportConformityExecutionSuccessTestCase<TTestHost>
    : ISignalTransportConformityExecutionTestCase<TTestHost>
    where TTestHost : ISignalTransportConformityTestHost<TTestHost>
{
    bool ShouldCompleteImmediately { get; }

    Task OnConnectionSuccess(TTestHost testHost, int numOfRuns) => Task.CompletedTask;

    Task OnReceiveSuccess(TTestHost testHost) => Task.CompletedTask;
}

public interface ISignalTransportConformityExecutionErrorTestCase<TTestHost>
    : ISignalTransportConformityExecutionTestCase<TTestHost>
    where TTestHost : ISignalTransportConformityTestHost<TTestHost>
{
    Exception? ConfigurationException { get; }

    Exception? PublishException { get; }

    IReadOnlyCollection<Exception?> HandlerExceptions { get; }

    int NumOfExpectedUnrecoverableConnectionErrors { get; }

    Task OnConfigurationException(TTestHost testHost) => Task.CompletedTask;

    Task OnPublishException(TTestHost testHost) => Task.CompletedTask;

    Task OnInitialConnection(TTestHost testHost) => Task.CompletedTask;

    Task OnHandlerExceptions(TTestHost testHost) => Task.CompletedTask;

    Task TriggerReconnect(TTestHost testHost) => Task.CompletedTask;

    Task AfterSuccessfulReconnect(TTestHost testHost) => Task.CompletedTask;
}

public interface ISignalTransportConformityContextTestCase<TTestHost> : ISignalTransportConformityTestCase<TTestHost>
    where TTestHost : ISignalTransportConformityTestHost<TTestHost>
{
    int NumOfReceivers { get; }

    bool HasActivity { get; }

    bool HasDownstreamData { get; }

    bool HasBidirectionalData { get; }

    Task OnConnectionSuccess(TTestHost testHost, int numOfRuns);
}
