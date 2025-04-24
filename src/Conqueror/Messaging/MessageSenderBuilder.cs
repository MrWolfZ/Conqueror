using System;

namespace Conqueror.Messaging;

internal sealed class MessageSenderBuilder<TMessage, TResponse>(
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext)
    : IMessageSenderBuilder<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ConquerorContext ConquerorContext { get; } = conquerorContext;
}
