namespace Conqueror.Transport.Http.Server.AspNetCore.Signalling.WebSockets;

internal sealed class HttpWebSocketsSignalPublisherFactory : IHttpWebSocketsSignalPublisherFactory
{
    public IHttpWebSocketsSignalPublisher<TSignal> Get<TSignal>()
        where TSignal : class, IHttpWebSocketsSignal<TSignal>
        => HttpWebSocketsSignalPublisher<TSignal>.Instance;
}
