namespace Conqueror.Transport.Http.Client.Signalling.WebSockets;

internal sealed class HttpWebSocketsSignalReceivers : IHttpWebSocketsSignalReceivers
{
    public ReceiverExecutionHandle RunReceivers(ISignalReceivers receivers, CancellationToken cancellationToken)
    {
        return receivers.RunReceivers(
            receivers.ServiceProvider.GetRequiredService<HttpWebSocketsSignalReceiverFactory>(),
            receivers.ServiceProvider.GetRequiredService<HttpWebSocketsSignalReceiverRunner>(),
            cancellationToken
        );
    }

    public ReceiverExecutionHandle RunReceiver<THandler>(
        ISignalReceivers receivers,
        CancellationToken cancellationToken
    )
        where THandler : class, IHttpWebSocketsSignalHandler, ISignalHandlerWithSourceGeneration
    {
        return receivers.RunReceiver<THandler, IHttpWebSocketsSignalHandlerTypesInjector, HttpWebSocketsSignalReceiver>(
            receivers.ServiceProvider.GetRequiredService<HttpWebSocketsSignalReceiverFactory>(),
            receivers.ServiceProvider.GetRequiredService<HttpWebSocketsSignalReceiverRunner>(),
            cancellationToken
        );
    }
}
