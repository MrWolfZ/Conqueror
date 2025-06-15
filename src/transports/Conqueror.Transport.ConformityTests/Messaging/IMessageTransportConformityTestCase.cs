namespace Conqueror.Transport.ConformityTests.Messaging;

public interface IMessageTransportConformityTestCase<out TTestHost> : ITransportConformityTestCase
    where TTestHost : IMessageTransportConformityTestHost
{
    TTestHost CreateTestHost();

    Task<IReadOnlyCollection<object>> SendMessages(IMessageSenders messageSenders, CancellationToken cancellationToken);
}

public interface IMessageTransportConformityExecutionTestCase<out TTestHost> : IMessageTransportConformityTestCase<TTestHost>
    where TTestHost : IMessageTransportConformityTestHost
{
    int NumOfReceivers { get; }

    bool MessagesAreSentInParallel { get; }

    IReadOnlyCollection<object> ExpectedReceivedMessages { get; }

    IReadOnlyCollection<object> ExpectedResponses { get; }
}

public interface IMessageTransportConformityExecutionSuccessTestCase<TTestHost> : IMessageTransportConformityExecutionTestCase<TTestHost>
    where TTestHost : IMessageTransportConformityTestHost
{
    bool ShouldCompleteImmediately { get; }

    Task BeforeSend(TTestHost host) => Task.CompletedTask;

    Task AfterMessagesAreReceived(TTestHost host) => Task.CompletedTask;
}

public interface IMessageTransportConformityExecutionErrorTestCase<TTestHost> : IMessageTransportConformityExecutionTestCase<TTestHost>
    where TTestHost : IMessageTransportConformityTestHost
{
    Exception? ReceiverConfigurationException { get; }

    Exception? SendException { get; }

    IReadOnlyCollection<Exception?> HandlerExceptions { get; }

    int? NumOfExpectedUnrecoverableConnectionErrors { get; }

    Task OnReceiverConfigurationException(TTestHost host) => Task.CompletedTask;

    Task OnSendException(TTestHost host) => Task.CompletedTask;

    Task OnInitialReceiverConnection(TTestHost testHost) => Task.CompletedTask;

    Task OnHandlerExceptions(TTestHost host) => Task.CompletedTask;

    Task TriggerReconnect(TTestHost host) => Task.CompletedTask;

    Task AfterSuccessfulReconnect(TTestHost host) => Task.CompletedTask;
}

public interface IMessageTransportConformityContextTestCase<TTestHost> : IMessageTransportConformityTestCase<TTestHost>
    where TTestHost : IMessageTransportConformityTestHost
{
    bool HasActivity { get; }

    bool HasDownstreamData { get; }

    bool HasBidirectionalData { get; }

    bool HasUpstreamData { get; }

    Task BeforePublish(TTestHost host) => Task.CompletedTask;
}
