using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public static class MessageHandlerExtensions
{
    public static IMessageHandler<TMessage, TResponse> WithPipeline<TMessage, TResponse>(this IMessageHandler<TMessage, TResponse> handler,
                                                                                         Action<IMessagePipeline<TMessage, TResponse>> configurePipeline)
        where TMessage : class, IMessage<TResponse>
    {
        if (handler is IConfigurableMessageHandler<TMessage, TResponse> c)
        {
            return c.WithPipeline(configurePipeline);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithPipeline)}", nameof(handler), null);
    }

    public static IMessageHandler<TMessage> WithPipeline<TMessage>(this IMessageHandler<TMessage> handler,
                                                                   Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline)
        where TMessage : class, IMessage<UnitMessageResponse>
    {
        if (handler is IConfigurableMessageHandler<TMessage> c)
        {
            return c.WithPipeline(configurePipeline);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithPipeline)}", nameof(handler), null);
    }

    public static IMessageHandler<TMessage, TResponse> WithTransport<TMessage, TResponse>(this IMessageHandler<TMessage, TResponse> handler,
                                                                                          ConfigureMessageTransportClient<TMessage, TResponse> configureTransport)
        where TMessage : class, IMessage<TResponse>
    {
        if (handler is IConfigurableMessageHandler<TMessage, TResponse> c)
        {
            return c.WithTransport(configureTransport);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithTransport)}", nameof(handler), null);
    }

    public static IMessageHandler<TMessage, TResponse> WithTransport<TMessage, TResponse>(this IMessageHandler<TMessage, TResponse> handler,
                                                                                          ConfigureMessageTransportClientAsync<TMessage, TResponse> configureTransport)
        where TMessage : class, IMessage<TResponse>
    {
        if (handler is IConfigurableMessageHandler<TMessage, TResponse> c)
        {
            return c.WithTransport(configureTransport);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithTransport)}", nameof(handler), null);
    }

    public static IMessageHandler<TMessage> WithTransport<TMessage>(this IMessageHandler<TMessage> handler,
                                                                    ConfigureMessageTransportClient<TMessage, UnitMessageResponse> configureTransport)
        where TMessage : class, IMessage<UnitMessageResponse>
    {
        if (handler is IConfigurableMessageHandler<TMessage> c)
        {
            return c.WithTransport(configureTransport);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithTransport)}", nameof(handler), null);
    }

    public static IMessageHandler<TMessage> WithTransport<TMessage>(this IMessageHandler<TMessage> handler,
                                                                    ConfigureMessageTransportClientAsync<TMessage, UnitMessageResponse> configureTransport)
        where TMessage : class, IMessage<UnitMessageResponse>
    {
        if (handler is IConfigurableMessageHandler<TMessage> c)
        {
            return c.WithTransport(configureTransport);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithTransport)}", nameof(handler), null);
    }
}
