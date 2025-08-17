namespace Conqueror;

public interface IHttpWebSocketsSignalPublisher<in TSignal> : ISignalPublisher<TSignal>
    where TSignal : class, IHttpWebSocketsSignal<TSignal>;
