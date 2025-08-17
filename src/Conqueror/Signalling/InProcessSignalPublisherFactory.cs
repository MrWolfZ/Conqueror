namespace Conqueror.Signalling;

internal sealed class InProcessSignalPublisherFactory : IInProcessSignalPublisherFactory
{
    public IInProcessSignalPublisher<TSignal> Get<TSignal>()
        where TSignal : class, ISignal<TSignal> => new InProcessSignalPublisher<TSignal>();
}
