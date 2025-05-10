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
    public Task Invoke(
        object signal,
        IServiceProvider serviceProvider,
        string transportTypeName,
        CancellationToken cancellationToken)
    {
        Debug.Assert(signal.GetType().IsAssignableTo(typeof(TSignal)), $"the signal type was expected to be assignable to '{typeof(TSignal)}', but was '{signal.GetType()}' instead.");

        var dispatcher = new SignalDispatcher<TSignal>(
            serviceProvider,
            new(new Publisher(handlerFn, transportTypeName)),
            configurePipeline,
            SignalTransportRole.Receiver,
            handlerType);

        return dispatcher.Dispatch((TSignal)signal, cancellationToken);
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
