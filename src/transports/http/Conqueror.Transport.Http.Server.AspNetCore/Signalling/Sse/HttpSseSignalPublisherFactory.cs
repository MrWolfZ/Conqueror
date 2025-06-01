namespace Conqueror.Transport.Http.Server.AspNetCore.Signalling.Sse;

internal sealed class HttpSseSignalPublisherFactory(HttpSseSignalBroker broker) : IHttpSseSignalPublisherFactory
{
    public IHttpSseSignalPublisher<TSignal> Get<TSignal>()
        where TSignal : class, IHttpSseSignal<TSignal>
    {
        return new HttpSseSignalPublisher<TSignal>(broker);
    }
}
