using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

internal sealed class DelegateMessageHandler<TMessage, TResponse>(
    MessageHandlerFn<TMessage, TResponse> handlerFn,
    IServiceProvider serviceProvider)
    : IMessageHandler<TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>
{
    public Task<TResponse> Handle(TMessage message, CancellationToken cancellationToken = default)
        => handlerFn(message, serviceProvider, cancellationToken);
}

internal sealed class DelegateMessageHandler<TMessage>(
    MessageHandlerFn<TMessage> handlerFn,
    IServiceProvider serviceProvider)
    : IMessageHandler<TMessage>
    where TMessage : class, IMessage<UnitMessageResponse>
{
    public Task Handle(TMessage message, CancellationToken cancellationToken = default)
        => handlerFn(message, serviceProvider, cancellationToken);
}
