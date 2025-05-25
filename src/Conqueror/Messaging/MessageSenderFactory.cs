using System;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal sealed class MessageSenderFactory<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    private readonly ConfigureMessageSenderAsync<TMessage, TResponse>? configureSenderAsync;
    private readonly ConfigureMessageSender<TMessage, TResponse>? configureSender;
    private readonly IMessageSender<TMessage, TResponse>? sender;

    public MessageSenderFactory(IMessageSender<TMessage, TResponse> sender)
    {
        this.sender = sender;
    }

    public MessageSenderFactory(ConfigureMessageSender<TMessage, TResponse>? configureSender)
    {
        this.configureSender = configureSender;
    }

    public MessageSenderFactory(ConfigureMessageSenderAsync<TMessage, TResponse>? configureSenderAsync)
    {
        this.configureSenderAsync = configureSenderAsync;
    }

    public ValueTask<IMessageSender<TMessage, TResponse>> Create(IServiceProvider serviceProvider, ConquerorContext conquerorContext)
    {
        if (sender is not null)
        {
            return ValueTask.FromResult(sender);
        }

        var transportBuilder = new MessageSenderBuilder<TMessage, TResponse>(serviceProvider, conquerorContext);

        if (configureSender is not null)
        {
            return ValueTask.FromResult(configureSender.Invoke(transportBuilder));
        }

        if (configureSenderAsync is not null)
        {
            return new(configureSenderAsync.Invoke(transportBuilder));
        }

        // this code should not be reachable
        throw new InvalidOperationException($"could not create transport client for message type '{typeof(TMessage)}' since it was not configured with a factory");
    }
}
