namespace Conqueror;

public interface IMessageReceiverFactory<in TTypesInjector, out TReceiver>
    where TTypesInjector : class, IMessageHandlerTypesInjector
    where TReceiver : class
{
    string TransportTypeName { get; }

    TReceiver? CreateReceiverForHandlerType(
        Type? handlerType,
        IReadOnlyCollection<IMessageReceiverHandlerInvoker<TTypesInjector>> invokers,
        TTypesInjector typesInjector
    );
}
