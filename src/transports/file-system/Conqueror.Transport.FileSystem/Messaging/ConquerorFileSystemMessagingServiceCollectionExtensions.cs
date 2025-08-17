#pragma warning disable IDE0130 // Namespaces don't match folder structure - it's a convention to place service collection extensions in this namespace

namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorFileSystemMessagingServiceCollectionExtensions
{
    public static IServiceCollection AddFileSystemMessageHandlerDelegate<TMessage, TResponse, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, TResponse, TIHandler> messageTypes,
        MessageHandlerFn<TMessage, TResponse> fn,
        Action<IFileSystemMessageReceiver> configureReceiver
    )
        where TMessage : class, IFileSystemMessage<TMessage, TResponse>
        where TIHandler : class, IFileSystemMessageHandler<TMessage, TResponse, TIHandler>
    {
        var typesInjector = new FileSystemMessageHandlerTypesInjector<TMessage, TResponse, TIHandler>(
            configureReceiver
        );

        return services.AddMessageHandlerDelegate(messageTypes, fn, configurePipeline: null, typesInjector);
    }

    public static IServiceCollection AddFileSystemMessageHandlerDelegate<TMessage, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, UnitMessageResponse, TIHandler> messageTypes,
        MessageHandlerFn<TMessage> fn,
        Action<IFileSystemMessageReceiver> configureReceiver
    )
        where TMessage : class, IFileSystemMessage<TMessage, UnitMessageResponse>
        where TIHandler : class, IFileSystemMessageHandler<TMessage, UnitMessageResponse, TIHandler>
    {
        var typesInjector = new FileSystemMessageHandlerTypesInjector<TMessage, UnitMessageResponse, TIHandler>(
            configureReceiver
        );

        return services.AddMessageHandlerDelegate(
            messageTypes,
            async (m, p, ct) =>
            {
                await fn(m, p, ct).ConfigureAwait(false);

                return UnitMessageResponse.Instance;
            },
            configurePipeline: null,
            typesInjector
        );
    }

    public static IServiceCollection AddFileSystemMessageHandlerDelegate<TMessage, TResponse, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, TResponse, TIHandler> messageTypes,
        MessageHandlerSyncFn<TMessage, TResponse> fn,
        Action<IFileSystemMessageReceiver> configureReceiver
    )
        where TMessage : class, IFileSystemMessage<TMessage, TResponse>
        where TIHandler : class, IFileSystemMessageHandler<TMessage, TResponse, TIHandler>
    {
        var typesInjector = new FileSystemMessageHandlerTypesInjector<TMessage, TResponse, TIHandler>(
            configureReceiver
        );

        return services.AddMessageHandlerDelegate(
            messageTypes,
            (m, p, _) => Task.FromResult(fn(m, p)),
            configurePipeline: null,
            typesInjector
        );
    }

    public static IServiceCollection AddFileSystemMessageHandlerDelegate<TMessage, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, UnitMessageResponse, TIHandler> messageTypes,
        MessageHandlerSyncFn<TMessage> fn,
        Action<IFileSystemMessageReceiver> configureReceiver
    )
        where TMessage : class, IFileSystemMessage<TMessage, UnitMessageResponse>
        where TIHandler : class, IFileSystemMessageHandler<TMessage, UnitMessageResponse, TIHandler>
    {
        var typesInjector = new FileSystemMessageHandlerTypesInjector<TMessage, UnitMessageResponse, TIHandler>(
            configureReceiver
        );

        return services.AddMessageHandlerDelegate(
            messageTypes,
            (m, p, _) =>
            {
                fn(m, p);

                return Task.FromResult(UnitMessageResponse.Instance);
            },
            configurePipeline: null,
            typesInjector
        );
    }

    public static IServiceCollection AddFileSystemMessageHandlerDelegate<TMessage, TResponse, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, TResponse, TIHandler> messageTypes,
        MessageHandlerFn<TMessage, TResponse> fn,
        Action<IMessagePipeline<TMessage, TResponse>> configurePipeline,
        Action<IFileSystemMessageReceiver> configureReceiver
    )
        where TMessage : class, IFileSystemMessage<TMessage, TResponse>
        where TIHandler : class, IFileSystemMessageHandler<TMessage, TResponse, TIHandler>
    {
        var typesInjector = new FileSystemMessageHandlerTypesInjector<TMessage, TResponse, TIHandler>(
            configureReceiver
        );

        return services.AddMessageHandlerDelegate(messageTypes, fn, configurePipeline, typesInjector);
    }

    public static IServiceCollection AddFileSystemMessageHandlerDelegate<TMessage, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, UnitMessageResponse, TIHandler> messageTypes,
        MessageHandlerFn<TMessage> fn,
        Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline,
        Action<IFileSystemMessageReceiver> configureReceiver
    )
        where TMessage : class, IFileSystemMessage<TMessage, UnitMessageResponse>
        where TIHandler : class, IFileSystemMessageHandler<TMessage, UnitMessageResponse, TIHandler>
    {
        var typesInjector = new FileSystemMessageHandlerTypesInjector<TMessage, UnitMessageResponse, TIHandler>(
            configureReceiver
        );

        return services.AddMessageHandlerDelegate(
            messageTypes,
            async (m, p, ct) =>
            {
                await fn(m, p, ct).ConfigureAwait(false);

                return UnitMessageResponse.Instance;
            },
            configurePipeline,
            typesInjector
        );
    }

    public static IServiceCollection AddFileSystemMessageHandlerDelegate<TMessage, TResponse, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, TResponse, TIHandler> messageTypes,
        MessageHandlerSyncFn<TMessage, TResponse> fn,
        Action<IMessagePipeline<TMessage, TResponse>> configurePipeline,
        Action<IFileSystemMessageReceiver> configureReceiver
    )
        where TMessage : class, IFileSystemMessage<TMessage, TResponse>
        where TIHandler : class, IFileSystemMessageHandler<TMessage, TResponse, TIHandler>
    {
        var typesInjector = new FileSystemMessageHandlerTypesInjector<TMessage, TResponse, TIHandler>(
            configureReceiver
        );

        return services.AddMessageHandlerDelegate(
            messageTypes,
            (m, p, _) => Task.FromResult(fn(m, p)),
            configurePipeline,
            typesInjector
        );
    }

    public static IServiceCollection AddFileSystemMessageHandlerDelegate<TMessage, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, UnitMessageResponse, TIHandler> messageTypes,
        MessageHandlerSyncFn<TMessage> fn,
        Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline,
        Action<IFileSystemMessageReceiver> configureReceiver
    )
        where TMessage : class, IFileSystemMessage<TMessage, UnitMessageResponse>
        where TIHandler : class, IFileSystemMessageHandler<TMessage, UnitMessageResponse, TIHandler>
    {
        var typesInjector = new FileSystemMessageHandlerTypesInjector<TMessage, UnitMessageResponse, TIHandler>(
            configureReceiver
        );

        return services.AddMessageHandlerDelegate(
            messageTypes,
            (m, p, _) =>
            {
                fn(m, p);

                return Task.FromResult(UnitMessageResponse.Instance);
            },
            configurePipeline,
            typesInjector
        );
    }
}
