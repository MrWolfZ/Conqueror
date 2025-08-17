namespace Conqueror.Transport.Http.Tests.Signalling;

public abstract class HttpSignalConformityTestCase
    : ISignalTransportConformityTestCase<HttpSignalTransportConformityTestHost>
{
    public enum HttpSignalTransportType
    {
        Sse,
        WebSockets,
    }

    public required HttpSignalTransportType TransportType { get; init; }

    public required Action<IServiceCollection> RegisterHandler { get; init; }

    public Action<IServiceCollection>? RegisterOnServer { get; init; }

    public required Func<ISignalReceivers, CancellationToken, ReceiverExecutionHandle> RunReceivers { get; init; }

    public required Func<ISignalPublishers, CancellationToken, Task> PublishSignals { get; init; }

    public Action<IHeaderDictionary>? ConfigureHeaders { get; init; }

    public required string Name { get; init; }

    public virtual HttpSignalTransportConformityTestHost CreateTestHost() =>
        HttpSignalTransportConformityTestHost.Create(this);

    Task ISignalTransportConformityTestCase<HttpSignalTransportConformityTestHost>.PublishSignals(
        ISignalPublishers publishers,
        CancellationToken cancellationToken
    ) => PublishSignals(publishers, cancellationToken);

    public virtual void RegisterServerServices(IServiceCollection services) { }

    public virtual void RegisterClientServices(IServiceCollection services) { }

    public virtual void ConfigureSseReceiver(
        HttpSignalTransportConformityTestHost host,
        IHttpSseSignalReceiver receiver
    )
    {
    }

    public virtual void ConfigureWebSocketsReceiver(
        HttpSignalTransportConformityTestHost host,
        IHttpWebSocketsSignalReceiver receiver
    )
    {
    }
}
