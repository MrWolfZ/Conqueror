using System;
using System.Threading.Tasks;
using Conqueror;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorHttpServerMessagingServiceCollectionExtensions
{
    public static IServiceCollection AddHttpMessageHandlerDelegate<TMessage, TResponse, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, TResponse, TIHandler> messageTypes,
        MessageHandlerFn<TMessage, TResponse> fn)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
    {
        var typesInjector = new HttpMessageHandlerTypesInjector<TMessage, TResponse, TIHandler>(null);
        return services.AddMessageHandlerDelegate(messageTypes, fn, null, typesInjector);
    }

    public static IServiceCollection AddHttpMessageHandlerDelegate<TMessage, TResponse, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, TResponse, TIHandler> messageTypes,
        MessageHandlerFn<TMessage, TResponse> fn,
        Action<IHttpMessageReceiver> configureReceiver)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
    {
        var typesInjector = new HttpMessageHandlerTypesInjector<TMessage, TResponse, TIHandler>(configureReceiver);
        return services.AddMessageHandlerDelegate(messageTypes, fn, null, typesInjector);
    }

    public static IServiceCollection AddHttpMessageHandlerDelegate<TMessage, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, UnitMessageResponse, TIHandler> messageTypes,
        MessageHandlerFn<TMessage> fn)
        where TMessage : class, IHttpMessage<TMessage, UnitMessageResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, UnitMessageResponse, TIHandler>
    {
        var typesInjector = new HttpMessageHandlerTypesInjector<TMessage, UnitMessageResponse, TIHandler>(null);
        return services.AddMessageHandlerDelegate(
            messageTypes,
            async (m, p, ct) =>
            {
                await fn(m, p, ct).ConfigureAwait(false);

                return UnitMessageResponse.Instance;
            },
            null, typesInjector);
    }

    public static IServiceCollection AddHttpMessageHandlerDelegate<TMessage, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, UnitMessageResponse, TIHandler> messageTypes,
        MessageHandlerFn<TMessage> fn,
        Action<IHttpMessageReceiver> configureReceiver)
        where TMessage : class, IHttpMessage<TMessage, UnitMessageResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, UnitMessageResponse, TIHandler>
    {
        var typesInjector = new HttpMessageHandlerTypesInjector<TMessage, UnitMessageResponse, TIHandler>(configureReceiver);
        return services.AddMessageHandlerDelegate(
            messageTypes,
            async (m, p, ct) =>
            {
                await fn(m, p, ct).ConfigureAwait(false);

                return UnitMessageResponse.Instance;
            },
            null, typesInjector);
    }

    public static IServiceCollection AddHttpMessageHandlerDelegate<TMessage, TResponse, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, TResponse, TIHandler> messageTypes,
        MessageHandlerSyncFn<TMessage, TResponse> fn)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
    {
        var typesInjector = new HttpMessageHandlerTypesInjector<TMessage, TResponse, TIHandler>(null);
        return services.AddMessageHandlerDelegate(messageTypes, (m, p, _) => Task.FromResult(fn(m, p)), null, typesInjector);
    }

    public static IServiceCollection AddHttpMessageHandlerDelegate<TMessage, TResponse, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, TResponse, TIHandler> messageTypes,
        MessageHandlerSyncFn<TMessage, TResponse> fn,
        Action<IHttpMessageReceiver> configureReceiver)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
    {
        var typesInjector = new HttpMessageHandlerTypesInjector<TMessage, TResponse, TIHandler>(configureReceiver);
        return services.AddMessageHandlerDelegate(messageTypes, (m, p, _) => Task.FromResult(fn(m, p)), null, typesInjector);
    }

    public static IServiceCollection AddHttpMessageHandlerDelegate<TMessage, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, UnitMessageResponse, TIHandler> messageTypes,
        MessageHandlerSyncFn<TMessage> fn)
        where TMessage : class, IHttpMessage<TMessage, UnitMessageResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, UnitMessageResponse, TIHandler>
    {
        var typesInjector = new HttpMessageHandlerTypesInjector<TMessage, UnitMessageResponse, TIHandler>(null);
        return services.AddMessageHandlerDelegate(
            messageTypes,
            (m, p, _) =>
            {
                fn(m, p);

                return Task.FromResult(UnitMessageResponse.Instance);
            },
            null, typesInjector);
    }

    public static IServiceCollection AddHttpMessageHandlerDelegate<TMessage, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, UnitMessageResponse, TIHandler> messageTypes,
        MessageHandlerSyncFn<TMessage> fn,
        Action<IHttpMessageReceiver> configureReceiver)
        where TMessage : class, IHttpMessage<TMessage, UnitMessageResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, UnitMessageResponse, TIHandler>
    {
        var typesInjector = new HttpMessageHandlerTypesInjector<TMessage, UnitMessageResponse, TIHandler>(configureReceiver);
        return services.AddMessageHandlerDelegate(
            messageTypes,
            (m, p, _) =>
            {
                fn(m, p);

                return Task.FromResult(UnitMessageResponse.Instance);
            },
            null, typesInjector);
    }

    public static IServiceCollection AddHttpMessageHandlerDelegate<TMessage, TResponse, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, TResponse, TIHandler> messageTypes,
        MessageHandlerFn<TMessage, TResponse> fn,
        Action<IMessagePipeline<TMessage, TResponse>> configurePipeline)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
    {
        var typesInjector = new HttpMessageHandlerTypesInjector<TMessage, TResponse, TIHandler>(null);
        return services.AddMessageHandlerDelegate(messageTypes, fn, configurePipeline, typesInjector);
    }

    public static IServiceCollection AddHttpMessageHandlerDelegate<TMessage, TResponse, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, TResponse, TIHandler> messageTypes,
        MessageHandlerFn<TMessage, TResponse> fn,
        Action<IMessagePipeline<TMessage, TResponse>> configurePipeline,
        Action<IHttpMessageReceiver> configureReceiver)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
    {
        var typesInjector = new HttpMessageHandlerTypesInjector<TMessage, TResponse, TIHandler>(configureReceiver);
        return services.AddMessageHandlerDelegate(messageTypes, fn, configurePipeline, typesInjector);
    }

    public static IServiceCollection AddHttpMessageHandlerDelegate<TMessage, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, UnitMessageResponse, TIHandler> messageTypes,
        MessageHandlerFn<TMessage> fn,
        Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline)
        where TMessage : class, IHttpMessage<TMessage, UnitMessageResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, UnitMessageResponse, TIHandler>
    {
        var typesInjector = new HttpMessageHandlerTypesInjector<TMessage, UnitMessageResponse, TIHandler>(null);
        return services.AddMessageHandlerDelegate(
            messageTypes,
            async (m, p, ct) =>
            {
                await fn(m, p, ct).ConfigureAwait(false);

                return UnitMessageResponse.Instance;
            },
            configurePipeline, typesInjector);
    }

    public static IServiceCollection AddHttpMessageHandlerDelegate<TMessage, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, UnitMessageResponse, TIHandler> messageTypes,
        MessageHandlerFn<TMessage> fn,
        Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline,
        Action<IHttpMessageReceiver> configureReceiver)
        where TMessage : class, IHttpMessage<TMessage, UnitMessageResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, UnitMessageResponse, TIHandler>
    {
        var typesInjector = new HttpMessageHandlerTypesInjector<TMessage, UnitMessageResponse, TIHandler>(configureReceiver);
        return services.AddMessageHandlerDelegate(
            messageTypes,
            async (m, p, ct) =>
            {
                await fn(m, p, ct).ConfigureAwait(false);

                return UnitMessageResponse.Instance;
            },
            configurePipeline, typesInjector);
    }

    public static IServiceCollection AddHttpMessageHandlerDelegate<TMessage, TResponse, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, TResponse, TIHandler> messageTypes,
        MessageHandlerSyncFn<TMessage, TResponse> fn,
        Action<IMessagePipeline<TMessage, TResponse>> configurePipeline)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
    {
        var typesInjector = new HttpMessageHandlerTypesInjector<TMessage, TResponse, TIHandler>(null);
        return services.AddMessageHandlerDelegate(messageTypes, (m, p, _) => Task.FromResult(fn(m, p)), configurePipeline, typesInjector);
    }

    public static IServiceCollection AddHttpMessageHandlerDelegate<TMessage, TResponse, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, TResponse, TIHandler> messageTypes,
        MessageHandlerSyncFn<TMessage, TResponse> fn,
        Action<IMessagePipeline<TMessage, TResponse>> configurePipeline,
        Action<IHttpMessageReceiver> configureReceiver)
        where TMessage : class, IHttpMessage<TMessage, TResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, TResponse, TIHandler>
    {
        var typesInjector = new HttpMessageHandlerTypesInjector<TMessage, TResponse, TIHandler>(configureReceiver);
        return services.AddMessageHandlerDelegate(messageTypes, (m, p, _) => Task.FromResult(fn(m, p)), configurePipeline, typesInjector);
    }

    public static IServiceCollection AddHttpMessageHandlerDelegate<TMessage, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, UnitMessageResponse, TIHandler> messageTypes,
        MessageHandlerSyncFn<TMessage> fn,
        Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline)
        where TMessage : class, IHttpMessage<TMessage, UnitMessageResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, UnitMessageResponse, TIHandler>
    {
        var typesInjector = new HttpMessageHandlerTypesInjector<TMessage, UnitMessageResponse, TIHandler>(null);
        return services.AddMessageHandlerDelegate(
            messageTypes,
            (m, p, _) =>
            {
                fn(m, p);

                return Task.FromResult(UnitMessageResponse.Instance);
            },
            configurePipeline, typesInjector);
    }

    public static IServiceCollection AddHttpMessageHandlerDelegate<TMessage, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, UnitMessageResponse, TIHandler> messageTypes,
        MessageHandlerSyncFn<TMessage> fn,
        Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline,
        Action<IHttpMessageReceiver> configureReceiver)
        where TMessage : class, IHttpMessage<TMessage, UnitMessageResponse>
        where TIHandler : class, IHttpMessageHandler<TMessage, UnitMessageResponse, TIHandler>
    {
        var typesInjector = new HttpMessageHandlerTypesInjector<TMessage, UnitMessageResponse, TIHandler>(configureReceiver);
        return services.AddMessageHandlerDelegate(
            messageTypes,
            (m, p, _) =>
            {
                fn(m, p);

                return Task.FromResult(UnitMessageResponse.Instance);
            },
            configurePipeline, typesInjector);
    }
}
