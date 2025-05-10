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

    public Task Invoke(
        object signal,
        IServiceProvider serviceProvider,
        string transportTypeName,
        CancellationToken cancellationToken)
    {
        if (!signal.GetType().IsAssignableTo(SignalType))
        {
            throw new InvalidOperationException($"the signal type was expected to be assignable to '{SignalType}', but was '{signal.GetType()}' instead");
        }

        return registration.Invoker.Invoke(
            signal,
            serviceProvider,
            transportTypeName,
            cancellationToken);
    }
}
