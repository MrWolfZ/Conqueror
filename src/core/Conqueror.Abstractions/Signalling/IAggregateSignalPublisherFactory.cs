namespace Conqueror;

public interface IAggregateSignalPublisherFactory
{
    IAggregateSignalPublisher<TSignal> Create<TSignal>(IReadOnlyCollection<ISignalPublisher<TSignal>> publishers)
        where TSignal : class, ISignal<TSignal>;
}
