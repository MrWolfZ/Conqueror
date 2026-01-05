namespace Conqueror.Signalling;

internal sealed class SignalReceiverHandlerInvoker<TTypesInjector>(
    SignalHandlerRegistration registration,
    ISignalHandlerInvoker handlerInvoker,
    TTypesInjector typesInjector
) : ISignalReceiverHandlerInvoker<TTypesInjector>
    where TTypesInjector : class, ISignalHandlerTypesInjector
{
    public Type SignalType { get; } = registration.SignalType;

    public Type? HandlerType { get; } = registration.HandlerType;

    public TTypesInjector TypesInjector { get; } = typesInjector;

    public Task Invoke(
        object signal,
        IServiceProvider serviceProvider,
        string transportTypeName,
        CancellationToken cancellationToken
    )
    {
        Debug.Assert(
            signal.GetType().IsAssignableTo(SignalType),
            $"the signal type was expected to be assignable to '{SignalType}', but was '{signal.GetType()}' instead"
        );

        return handlerInvoker.Invoke(signal, serviceProvider, transportTypeName, cancellationToken);
    }
}
