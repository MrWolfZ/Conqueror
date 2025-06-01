using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Signalling;

internal sealed class SignalHandlerInvoker<TSignal>(
    Action<ISignalPipeline<TSignal>>? configurePipeline,
    SignalHandlerFn<TSignal> handlerFn,
    Type? handlerType)
    : ISignalHandlerInvoker
    where TSignal : class, ISignal<TSignal>
{
    // since the dispatcher only relies on singleton services, we can cache it here to avoid unnecessary allocations
    private SignalDispatcher? dispatcher;

    public Task Invoke(
        object signal,
        IServiceProvider serviceProvider,
        string transportTypeName,
        CancellationToken cancellationToken)
    {
        Debug.Assert(
            signal.GetType().IsAssignableTo(typeof(TSignal)),
            $"the signal type was expected to be assignable to '{typeof(TSignal)}', but was '{signal.GetType()}' instead.");

        dispatcher ??= new(
            serviceProvider.GetRequiredService<IConquerorContextAccessor>(),
            serviceProvider.GetRequiredService<ISignalIdFactory>(),
            SignalTransportRole.Receiver,
            handlerType);

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
