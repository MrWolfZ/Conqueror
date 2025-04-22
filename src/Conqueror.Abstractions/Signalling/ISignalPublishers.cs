// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface ISignalPublishers
{
    TIHandler For<TSignal, TIHandler>()
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>;

    TIHandler For<TSignal, TIHandler>(SignalTypes<TSignal, TIHandler> signalTypes)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>;
}
