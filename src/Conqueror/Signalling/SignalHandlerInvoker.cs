using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class SignalHandlerInvoker<TSignal>(
    Action<ISignalPipeline<TSignal>>? configurePipeline,
    SignalHandlerFn<TSignal> handlerFn)
    : ISignalHandlerInvoker
    where TSignal : class, ISignal<TSignal>
{
    public Task Invoke(IServiceProvider serviceProvider, object signal, string transportTypeName, CancellationToken cancellationToken)
    {
        var dispatcher = new SignalDispatcher<TSignal>(serviceProvider,
                                                       new(new Publisher(handlerFn, transportTypeName)),
                                                       configurePipeline,
                                                       SignalTransportRole.Receiver);

        return dispatcher.Dispatch((TSignal)signal, cancellationToken);
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
    Task Invoke(
        IServiceProvider serviceProvider,
        object signal,
        string transportTypeName,
        CancellationToken cancellationToken);
}
