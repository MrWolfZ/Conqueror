namespace Conqueror.Signalling;

internal sealed class AggregateSignalPublisherFactory : IAggregateSignalPublisherFactory
{
    public IAggregateSignalPublisher<TSignal> Create<TSignal>(IReadOnlyCollection<ISignalPublisher<TSignal>> publishers)
        where TSignal : class, ISignal<TSignal> => new AggregateSignalPublisher<TSignal>(publishers);
}
