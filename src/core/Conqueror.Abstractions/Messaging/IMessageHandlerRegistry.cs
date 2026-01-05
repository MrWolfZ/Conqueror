namespace Conqueror;

public interface IMessageHandlerRegistry
{
    IMessageReceiverHandlerInvoker<TTypesInjector>? GetReceiverHandlerInvoker<TMessage, TResponse, TTypesInjector>()
        where TMessage : class, IMessage<TMessage, TResponse>
        where TTypesInjector : class, IMessageHandlerTypesInjector;

    IReadOnlyCollection<IMessageReceiverHandlerInvoker<TTypesInjector>> GetReceiverHandlerInvokers<TTypesInjector>()
        where TTypesInjector : class, IMessageHandlerTypesInjector;
}
