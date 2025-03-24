using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal sealed class DelegateMessageHandler<TMessage, TResponse>(
    MessageHandlerFn<TMessage, TResponse> handlerFn,
    IServiceProvider serviceProvider)
    : IMessageHandler<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public Task<TResponse> Handle(TMessage message, CancellationToken cancellationToken = default)
        => handlerFn(message, serviceProvider, cancellationToken);
}

internal sealed class DelegateMessageHandler<TMessage>(
    MessageHandlerFn<TMessage> handlerFn,
    IServiceProvider serviceProvider)
    : IMessageHandler<TMessage, UnitMessageResponse>
    where TMessage : class, IMessage<TMessage, UnitMessageResponse>
{
    public async Task<UnitMessageResponse> Handle(TMessage message, CancellationToken cancellationToken = default)
    {
        await handlerFn(message, serviceProvider, cancellationToken).ConfigureAwait(false);
        return UnitMessageResponse.Instance;
    }
}
