// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IHttpWebSocketsSignalPublisherFactory
{
    IHttpWebSocketsSignalPublisher<TSignal> Get<TSignal>()
        where TSignal : class, IHttpWebSocketsSignal<TSignal>;
}
