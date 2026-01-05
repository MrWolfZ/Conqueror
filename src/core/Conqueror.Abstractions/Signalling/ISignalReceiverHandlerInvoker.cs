namespace Conqueror;

public interface ISignalReceiverHandlerInvoker
{
    Type SignalType { get; }

    Type? HandlerType { get; }

    Task Invoke(
        object signal,
        IServiceProvider serviceProvider,
        string transportTypeName,
        CancellationToken cancellationToken
    );
}

public interface ISignalReceiverHandlerInvoker<out TTypesInjector> : ISignalReceiverHandlerInvoker
    where TTypesInjector : class, ISignalHandlerTypesInjector
{
    TTypesInjector TypesInjector { get; }
}
