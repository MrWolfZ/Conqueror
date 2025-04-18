using System;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public static class MessageHandlerExtensions
{
    public static IMessageHandler<TMessage, TResponse> WithPipeline<TMessage, TResponse>(this IMessageHandler<TMessage, TResponse> handler,
                                                                                         Action<IMessagePipeline<TMessage, TResponse>> configurePipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        if (handler is IConfigurableMessageHandler<TMessage, TResponse> c)
        {
            return c.WithPipeline(configurePipeline);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithPipeline)}", nameof(handler), null);
    }

    public static IMessageHandler<TMessage> WithPipeline<TMessage>(this IMessageHandler<TMessage> handler,
                                                                   Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline)
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
    {
        if (handler is IConfigurableMessageHandler<TMessage> c)
        {
            return c.WithPipeline(configurePipeline);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithPipeline)}", nameof(handler), null);
    }

    public static IMessageHandler<TMessage, TResponse> WithTransport<TMessage, TResponse>(this IMessageHandler<TMessage, TResponse> handler,
                                                                                          ConfigureMessageTransportClient<TMessage, TResponse> configureTransport)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        if (handler is IConfigurableMessageHandler<TMessage, TResponse> c)
        {
            return c.WithTransport(configureTransport);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithTransport)}", nameof(handler), null);
    }

    public static IMessageHandler<TMessage, TResponse> WithTransport<TMessage, TResponse>(this IMessageHandler<TMessage, TResponse> handler,
                                                                                          ConfigureMessageTransportClientAsync<TMessage, TResponse> configureTransport)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        if (handler is IConfigurableMessageHandler<TMessage, TResponse> c)
        {
            return c.WithTransport(configureTransport);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithTransport)}", nameof(handler), null);
    }

    public static IMessageHandler<TMessage> WithTransport<TMessage>(this IMessageHandler<TMessage> handler,
                                                                    ConfigureMessageTransportClient<TMessage, UnitMessageResponse> configureTransport)
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
    {
        if (handler is IConfigurableMessageHandler<TMessage> c)
        {
            return c.WithTransport(configureTransport);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithTransport)}", nameof(handler), null);
    }

    public static IMessageHandler<TMessage> WithTransport<TMessage>(this IMessageHandler<TMessage> handler,
                                                                    ConfigureMessageTransportClientAsync<TMessage, UnitMessageResponse> configureTransport)
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
    {
        if (handler is IConfigurableMessageHandler<TMessage> c)
        {
            return c.WithTransport(configureTransport);
        }

        throw new ArgumentException($"handler type '{handler.GetType()}' is not supported in {nameof(WithTransport)}", nameof(handler), null);
    }

    public static THandler AsIHandler<TMessage, TResponse, THandler>(this IMessageHandler<TMessage, TResponse> handler)
        where TMessage : class, IMessage<TMessage, TResponse>
        where THandler : class, IMessageHandler<TMessage, TResponse>, IGeneratedMessageHandler
    {
        return TMessage.DefaultTypeInjector.CreateWithMessageTypes(new HandlerCastInjectable<THandler>(handler));
    }

    public static THandler AsIHandler<TMessage, THandler>(this IMessageHandler<TMessage> handler)
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
        where THandler : class, IMessageHandler<TMessage>, IGeneratedMessageHandler
    {
        return TMessage.DefaultTypeInjector.CreateWithMessageTypes(new HandlerCastInjectable<THandler>(handler));
    }

    private sealed class HandlerCastInjectable<THandler>(object handler) : IDefaultMessageTypesInjectable<THandler>
        where THandler : class
    {
        public THandler WithInjectedTypes<TMessage, TResponse, TGeneratedHandlerInterface, TGeneratedHandlerAdapter, TPipelineInterface, TPipelineAdapter>()
            where TMessage : class, IMessage<TMessage, TResponse>
            where TGeneratedHandlerInterface : class, IGeneratedMessageHandler<TMessage, TResponse, TPipelineInterface>
            where TGeneratedHandlerAdapter : GeneratedMessageHandlerAdapter<TMessage, TResponse>, TGeneratedHandlerInterface, new()
            where TPipelineInterface : class, IMessagePipeline<TMessage, TResponse>
            where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, TResponse>, TPipelineInterface, new()
        {
            Debug.Assert(typeof(TGeneratedHandlerInterface) == typeof(THandler), "result handler type should always be equal to generated handler interface");
            Debug.Assert(handler is IMessageHandler<TMessage, TResponse>, "handler to wrap must be of correct type");

            var adapter = new TGeneratedHandlerAdapter
            {
                Wrapped = (IMessageHandler<TMessage, TResponse>)handler,
            };

            return adapter as THandler ?? throw new InvalidOperationException("could not create handler adapter");
        }

        public THandler WithInjectedTypes<TMessage, TGeneratedHandlerInterface, TGeneratedHandlerAdapter, TPipelineInterface, TPipelineAdapter>()
            where TMessage : class, IMessage<TMessage, UnitMessageResponse>
            where TGeneratedHandlerInterface : class, IGeneratedMessageHandler<TMessage, TPipelineInterface>
            where TGeneratedHandlerAdapter : GeneratedMessageHandlerAdapter<TMessage>, TGeneratedHandlerInterface, new()
            where TPipelineInterface : class, IMessagePipeline<TMessage, UnitMessageResponse>
            where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, UnitMessageResponse>, TPipelineInterface, new()
        {
            Debug.Assert(typeof(TGeneratedHandlerInterface) == typeof(THandler), "result handler type should always be equal to generated handler interface");
            Debug.Assert(handler is IMessageHandler<TMessage>, "handler to wrap must be of correct type");

            var adapter = new TGeneratedHandlerAdapter
            {
                Wrapped = (IMessageHandler<TMessage>)handler,
            };

            return adapter as THandler ?? throw new InvalidOperationException("could not create handler adapter");
        }
    }
}
