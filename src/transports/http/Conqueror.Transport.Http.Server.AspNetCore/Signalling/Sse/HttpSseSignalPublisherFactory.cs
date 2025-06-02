namespace Conqueror.Transport.Http.Server.AspNetCore.Signalling.Sse;

internal sealed class HttpSseSignalPublisherFactory : IHttpSseSignalPublisherFactory
{
    public IHttpSseSignalPublisher<TSignal> Get<TSignal>()
        where TSignal : class, IHttpSseSignal<TSignal>
        => HttpSseSignalPublisher<TSignal>.Instance;
}
