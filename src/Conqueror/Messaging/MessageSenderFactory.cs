using System;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal sealed class MessageSenderFactory<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    private readonly ConfigureMessageSenderAsync<TMessage, TResponse>? asyncSenderFactory;
    private readonly ConfigureMessageSender<TMessage, TResponse>? syncSenderFactory;
    private readonly IMessageSender<TMessage, TResponse>? sender;

    public MessageSenderFactory(IMessageSender<TMessage, TResponse> sender)
    {
        this.sender = sender;
    }

    public MessageSenderFactory(ConfigureMessageSender<TMessage, TResponse>? syncSenderFactory)
    {
        this.syncSenderFactory = syncSenderFactory;
    }

    public MessageSenderFactory(ConfigureMessageSenderAsync<TMessage, TResponse>? asyncSenderFactory)
    {
        this.asyncSenderFactory = asyncSenderFactory;
    }

    public Task<IMessageSender<TMessage, TResponse>> Create(IServiceProvider serviceProvider, ConquerorContext conquerorContext)
    {
        if (sender is not null)
        {
            return Task.FromResult(sender);
        }

        var transportBuilder = new MessageSenderBuilder<TMessage, TResponse>(serviceProvider, conquerorContext);

        if (syncSenderFactory is not null)
        {
            return Task.FromResult(syncSenderFactory.Invoke(transportBuilder));
        }

        if (asyncSenderFactory is not null)
        {
            return asyncSenderFactory.Invoke(transportBuilder);
        }

        // this code should not be reachable
        throw new InvalidOperationException($"could not create transport client for message type '{typeof(TMessage)}' since it was not configured with a factory");
    }
}
