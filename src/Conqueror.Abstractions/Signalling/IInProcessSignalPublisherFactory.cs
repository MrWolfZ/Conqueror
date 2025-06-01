// ReSharper disable once CheckNamespace

namespace Conqueror;

public interface IInProcessSignalPublisherFactory
{
    IInProcessSignalPublisher<TSignal> Get<TSignal>()
        where TSignal : class, ISignal<TSignal>;
}
