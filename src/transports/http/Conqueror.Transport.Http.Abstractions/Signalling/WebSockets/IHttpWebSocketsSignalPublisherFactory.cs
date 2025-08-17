namespace Conqueror;

public interface IHttpWebSocketsSignalPublisherFactory
{
    IHttpWebSocketsSignalPublisher<TSignal> Get<TSignal>()
        where TSignal : class, IHttpWebSocketsSignal<TSignal>;
}
