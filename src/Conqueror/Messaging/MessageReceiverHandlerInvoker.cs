using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal sealed class MessageReceiverHandlerInvoker<TTypesInjector>(
    MessageHandlerRegistration registration,
    IMessageHandlerInvoker handlerInvoker,
    TTypesInjector typesInjector)
    : IMessageReceiverHandlerInvoker<TTypesInjector>
    where TTypesInjector : class, IMessageHandlerTypesInjector
{
    public Type MessageType { get; } = registration.MessageType;

    public Type ResponseType { get; } = registration.ResponseType;

    public Type? HandlerType { get; } = registration.HandlerType;

    public TTypesInjector TypesInjector { get; } = typesInjector;

    public Task<TResponse> Invoke<TMessage, TResponse>(TMessage message,
                                                       IServiceProvider serviceProvider,
                                                       string transportTypeName,
                                                       CancellationToken cancellationToken)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return handlerInvoker.Invoke<TMessage, TResponse>(message, serviceProvider, transportTypeName, cancellationToken);
    }
}
