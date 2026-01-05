namespace Conqueror;

public interface ISignalPublishers
{
    TIHandler For<TSignal, TIHandler>(SignalTypes<TSignal, TIHandler> signalTypes)
        where TSignal : class, ISignal<TSignal>
        where TIHandler : class, ISignalHandler<TSignal, TIHandler>;
}
