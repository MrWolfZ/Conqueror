namespace Conqueror.Transport.ConformityTests.Messaging;

public interface IMessageTransportConformityTestHost : ITransportConformityTestHost
{
    Task<IMessageTransportConformityReceiverTestHost> CreateReceiverTestHost(
        CancellationToken cancellationToken,
        Func<object, ConquerorContext, CancellationToken, Task>? messageCallback = null
    );

    Task<IMessageTransportConformitySenderTestHost> CreateSenderTestHost(
        CancellationToken cancellationToken,
        Func<object, ConquerorContext, CancellationToken, Task>? sendCallback = null
    );
}

public interface IMessageTransportConformitySenderTestHost : IAsyncDisposable
{
    IMessageSenders MessageSenders { get; }

    IConquerorContextAccessor ConquerorContextAccessor { get; }
}

public interface IMessageTransportConformityReceiverTestHost : IAsyncDisposable
{
    ReceiverExecutionHandle? ReceiverExecutionHandle { get; }
}
