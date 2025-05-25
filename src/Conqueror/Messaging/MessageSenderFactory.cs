using System;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal sealed class MessageSenderFactory<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public static readonly MessageSenderFactory<TMessage, TResponse> InProcess = new(static b => b.UseInProcess());

    private readonly ConfigureMessageSender<TMessage, TResponse>? configureSender;
    private readonly ConfigureMessageSenderAsync<TMessage, TResponse>? configureSenderAsync;
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

    public IMessageSender<TMessage, TResponse>? CreateSync(IServiceProvider serviceProvider, ConquerorContext conquerorContext)
    {
        if (sender is not null)
        {
            return sender;
        }

        var transportBuilder = new MessageSenderBuilder<TMessage, TResponse>(serviceProvider, conquerorContext);
        return configureSender?.Invoke(transportBuilder);
    }

    public ValueTask<IMessageSender<TMessage, TResponse>> CreateAsync(IServiceProvider serviceProvider, ConquerorContext conquerorContext)
    {
        if (configureSenderAsync is not null)
        {
            var transportBuilder = new MessageSenderBuilder<TMessage, TResponse>(serviceProvider, conquerorContext);
            return new(configureSenderAsync.Invoke(transportBuilder));
        }

        // this code should not be reachable
        throw new InvalidOperationException(
            $"could not create transport client for message type '{typeof(TMessage)}' since it was not configured with a factory");
    }
}
