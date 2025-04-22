using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface ISignalPublishers
{
    THandler For<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TSignal,
        THandler>()
        where TSignal : class, ISignal<TSignal>
        where THandler : class, ISignalHandler<TSignal, THandler>;

    THandler For<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        TSignal,
        THandler>(SignalTypes<TSignal, THandler> signalTypes)
        where TSignal : class, ISignal<TSignal>
        where THandler : class, ISignalHandler<TSignal, THandler>;
}
