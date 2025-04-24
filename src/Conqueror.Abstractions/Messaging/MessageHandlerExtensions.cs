using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public static class MessageHandlerExtensions
{
    public static TIHandler WithPipeline<TMessage, TResponse, TIHandler>(this IMessageHandler<TMessage, TResponse, TIHandler> handler,
                                                                         Action<IMessagePipeline<TMessage, TResponse>> configurePipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
    {
        if (handler is IMessageHandlerProxy<TMessage, TResponse, TIHandler> c)
        {
            return c.WithPipeline(configurePipeline);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithPipeline)}", nameof(handler), null);
    }

    public static TIHandler WithTransport<TMessage, TResponse, TIHandler>(this IMessageHandler<TMessage, TResponse, TIHandler> handler,
                                                                          ConfigureMessageSender<TMessage, TResponse> configureTransport)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
    {
        if (handler is IMessageHandlerProxy<TMessage, TResponse, TIHandler> c)
        {
            return c.WithTransport(configureTransport);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithTransport)}", nameof(handler), null);
    }

    public static TIHandler WithTransport<TMessage, TResponse, TIHandler>(this IMessageHandler<TMessage, TResponse, TIHandler> handler,
                                                                          ConfigureMessageSenderAsync<TMessage, TResponse> configureTransport)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
    {
        if (handler is IMessageHandlerProxy<TMessage, TResponse, TIHandler> c)
        {
            return c.WithTransport(configureTransport);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithTransport)}", nameof(handler), null);
    }
}
