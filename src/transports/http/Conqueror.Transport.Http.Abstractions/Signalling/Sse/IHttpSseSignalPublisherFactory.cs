namespace Conqueror;

public interface IHttpSseSignalPublisherFactory
{
    IHttpSseSignalPublisher<TSignal> Get<TSignal>()
        where TSignal : class, IHttpSseSignal<TSignal>;
}
