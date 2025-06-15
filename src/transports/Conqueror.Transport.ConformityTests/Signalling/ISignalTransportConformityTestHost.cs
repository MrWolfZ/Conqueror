namespace Conqueror.Transport.ConformityTests.Signalling;

public interface ISignalTransportConformityTestHost : ITransportConformityTestHost
{
    Task<ISignalTransportConformityReceiverTestHost> CreateReceiverTestHost(
        CancellationToken cancellationToken,
        Func<object, ConquerorContext, CancellationToken, Task>? signalCallback = null);

    Task<ISignalTransportConformityPublisherTestHost> CreatePublisherTestHost(
        CancellationToken cancellationToken,
        Func<object, ConquerorContext, CancellationToken, Task>? publishCallback = null);
}

public interface ISignalTransportConformityPublisherTestHost : IAsyncDisposable
{
    ISignalPublishers SignalPublishers { get; }

    IConquerorContextAccessor ConquerorContextAccessor { get; }
}

public interface ISignalTransportConformityReceiverTestHost : IAsyncDisposable
{
    ReceiverExecutionHandle? ReceiverExecutionHandle { get; }
}
