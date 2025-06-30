using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class SignalHandlerInvoker<TSignal>(
    IConquerorContextAccessor conquerorContextAccessor,
    ISignalIdFactory signalIdFactory,
    Action<ISignalPipeline<TSignal>>? configurePipeline,
    SignalHandlerFn<TSignal> handlerFn,
    Type? handlerType)
    : ISignalHandlerInvoker
    where TSignal : class, ISignal<TSignal>
{
    private readonly SignalDispatcher dispatcher = new(
        conquerorContextAccessor,
        signalIdFactory,
        SignalTransportRole.Receiver,
        handlerType);

    public Task Invoke(
        object signal,
        IServiceProvider serviceProvider,
        string transportTypeName,
        CancellationToken cancellationToken)
    {
        Debug.Assert(
            signal.GetType().IsAssignableTo(typeof(TSignal)),
            $"the signal type was expected to be assignable to '{typeof(TSignal)}', but was '{signal.GetType()}' instead.");

        return dispatcher.Dispatch(
            (TSignal)signal,
            serviceProvider,
            configurePipeline,
            new Publisher(handlerFn, transportTypeName),
            configurePublisher: null,
            configurePublisherAsync: null,
            cancellationToken);
    }

    private sealed class Publisher(SignalHandlerFn<TSignal> handlerFn, string transportTypeName) : ISignalPublisher<TSignal>
    {
        public string TransportTypeName { get; } = transportTypeName;

        public Task Publish(
            TSignal signal,
            IServiceProvider serviceProvider,
            ConquerorContext conquerorContext,
            CancellationToken cancellationToken)
            => handlerFn(signal, serviceProvider, cancellationToken);
    }
}

internal interface ISignalHandlerInvoker
{
    Task Invoke(
        object signal,
        IServiceProvider serviceProvider,
        string transportTypeName,
        CancellationToken cancellationToken);
}
