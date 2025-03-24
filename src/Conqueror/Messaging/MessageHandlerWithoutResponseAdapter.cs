using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Messaging;

internal sealed class MessageHandlerWithoutResponseAdapter<TMessage>(
    Type handlerType,
    IServiceProvider serviceProvider)
    : IMessageHandler<TMessage, UnitMessageResponse>
    where TMessage : class, IMessage<TMessage, UnitMessageResponse>
{
    public async Task<UnitMessageResponse> Handle(TMessage message, CancellationToken cancellationToken = default)
    {
        var handler = serviceProvider.GetRequiredService(handlerType);
        await ((IMessageHandler<TMessage>)handler).Handle(message, cancellationToken).ConfigureAwait(false);
        return UnitMessageResponse.Instance;
    }
}
