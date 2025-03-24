using System;

namespace Conqueror.Messaging;

internal sealed class MessageTransportClientBuilder<TMessage, TResponse>(
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext)
    : IMessageTransportClientBuilder<TMessage, TResponse>
    where TMessage : class, IMessage<TResponse>
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ConquerorContext ConquerorContext { get; } = conquerorContext;
}
