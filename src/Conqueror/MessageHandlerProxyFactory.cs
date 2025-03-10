using System;

namespace Conqueror;

internal sealed class MessageHandlerProxyFactory(
    IServiceProvider serviceProvider,
    MessageTransportRole transportRole)
    : IMessageHandlerProxyFactory
{
    public IConfigurableMessageHandler<TMessage, TResponse> CreateProxy<TMessage, TResponse>()
        where TMessage : class, IMessage<TResponse>
    {
        return new MessageHandlerProxy<TMessage, TResponse>(
            serviceProvider,
            new(b => b.UseInProcess()),
            null,
            transportRole);
    }
}
