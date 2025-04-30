using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class SignalHandlerInvoker<TSignal>(
    Action<ISignalPipeline<TSignal>>? configurePipeline,
    SignalHandlerFn<TSignal> handlerFn,
    Type? handlerType)
    : ISignalHandlerInvoker
    where TSignal : class, ISignal<TSignal>
{
    public Task Invoke<T>(IServiceProvider serviceProvider, T signal, string transportTypeName, CancellationToken cancellationToken)
        where T : class, ISignal<T>
    {
        Debug.Assert(typeof(T) == typeof(TSignal), $"the signal type was expected to be {typeof(TSignal)}, but was {typeof(T)} instead.");

        var dispatcher = new SignalDispatcher<TSignal>(serviceProvider,
                                                       new(new Publisher(handlerFn, transportTypeName)),
                                                       configurePipeline,
                                                       SignalTransportRole.Receiver,
                                                       handlerType);

        return dispatcher.Dispatch((signal as TSignal)!, cancellationToken);
    }

    private sealed class Publisher(SignalHandlerFn<TSignal> handlerFn, string transportTypeName) : ISignalPublisher<TSignal>
    {
        public string TransportTypeName { get; } = transportTypeName;

        public Task Publish(TSignal signal, IServiceProvider serviceProvider, ConquerorContext conquerorContext, CancellationToken cancellationToken)
            => handlerFn(signal, serviceProvider, cancellationToken);
    }
}

internal interface ISignalHandlerInvoker
{
    Task Invoke<TSignal>(
        IServiceProvider serviceProvider,
        TSignal signal,
        string transportTypeName,
        CancellationToken cancellationToken)
        where TSignal : class, ISignal<TSignal>;
}
