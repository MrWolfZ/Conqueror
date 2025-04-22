using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class SignalReceiverHandlerInvoker<TTypesInjector>(
    SignalHandlerRegistration registration,
    TTypesInjector typesInjector)
    : ISignalReceiverHandlerInvoker<TTypesInjector>
    where TTypesInjector : class, ISignalTypesInjector
{
    public Type SignalType { get; } = registration.SignalType;

    public Type? HandlerType { get; } = registration.HandlerType;

    public TTypesInjector TypesInjector { get; } = typesInjector;

    public Task Invoke<TSignal>(IServiceProvider serviceProvider,
                                TSignal signal,
                                string transportTypeName,
                                CancellationToken cancellationToken)
        where TSignal : class, ISignal<TSignal>
    {
        return registration.Invoker.Invoke(serviceProvider, signal, transportTypeName, cancellationToken);
    }
}
