using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class SignalReceiverHandlerInvoker<TTypesInjector>(
    SignalHandlerRegistration registration,
    TTypesInjector typesInjector)
    : ISignalReceiverHandlerInvoker<TTypesInjector>
    where TTypesInjector : class, ISignalHandlerTypesInjector
{
    public Type SignalType { get; } = registration.SignalType;

    public Type? HandlerType { get; } = registration.HandlerType;

    public TTypesInjector TypesInjector { get; } = typesInjector;

    public Task Invoke<TSignal>(TSignal signal,
                                IServiceProvider serviceProvider,
                                string transportTypeName,
                                CancellationToken cancellationToken)
        where TSignal : class, ISignal<TSignal>
    {
        return registration.Invoker.Invoke(serviceProvider, signal, transportTypeName, cancellationToken);
    }
}
