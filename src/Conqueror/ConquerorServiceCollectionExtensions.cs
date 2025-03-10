using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Conqueror;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorServiceCollectionExtensions
{
    public static IServiceCollection AddConqueror(this IServiceCollection services)
    {
        services.TryAddTransient<IMessageClients, MessageClients>();
        services.TryAddSingleton<MessageTransportRegistry>();
        services.TryAddSingleton<IMessageTransportRegistry>(p => p.GetRequiredService<MessageTransportRegistry>());

        services.AddConquerorContext();

        return services;
    }

    public static IServiceCollection AddConquerorMessageHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services)
        where THandler : class, IMessageHandler, IGeneratedMessageHandler
    {
        return services.AddConquerorMessageHandlerInternal<THandler>(new(typeof(THandler), typeof(THandler), ServiceLifetime.Transient));
    }

    public static IServiceCollection AddConquerorMessageHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime)
        where THandler : class, IMessageHandler, IGeneratedMessageHandler
    {
        return services.AddConquerorMessageHandlerInternal<THandler>(new(typeof(THandler), typeof(THandler), lifetime));
    }

    public static IServiceCollection AddConquerorMessageHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        Func<IServiceProvider, THandler> factory)
        where THandler : class, IMessageHandler, IGeneratedMessageHandler
    {
        return services.AddConquerorMessageHandlerInternal<THandler>(new(typeof(THandler), factory, ServiceLifetime.Transient));
    }

    public static IServiceCollection AddConquerorMessageHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        Func<IServiceProvider, THandler> factory,
        ServiceLifetime lifetime)
        where THandler : class, IMessageHandler, IGeneratedMessageHandler
    {
        return services.AddConquerorMessageHandlerInternal<THandler>(new(typeof(THandler), factory, lifetime));
    }

    public static IServiceCollection AddConquerorMessageHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        THandler instance)
        where THandler : class, IMessageHandler, IGeneratedMessageHandler
    {
        return services.AddConquerorMessageHandlerInternal<THandler>(new(typeof(THandler), instance));
    }

    public static IServiceCollection AddConquerorMessageHandlerDelegate<TMessage, TResponse>(this IServiceCollection services, MessageHandlerFn<TMessage, TResponse> fn)
        where TMessage : class, IMessage<TResponse>
    {
        return services.AddConquerorMessageHandlerDelegateInternal(fn, null);
    }

    public static IServiceCollection AddConquerorMessageHandlerDelegate<TMessage>(this IServiceCollection services, MessageHandlerFn<TMessage> fn)
        where TMessage : class, IMessage<UnitMessageResponse>
    {
        return services.AddConquerorMessageHandlerDelegateInternal<TMessage, UnitMessageResponse>(async (m, p, ct) =>
        {
            await fn(m, p, ct).ConfigureAwait(false);
            return UnitMessageResponse.Instance;
        }, null);
    }

    public static IServiceCollection AddConquerorMessageHandlerDelegate<TMessage, TResponse>(this IServiceCollection services, MessageHandlerSyncFn<TMessage, TResponse> fn)
        where TMessage : class, IMessage<TResponse>
    {
        return services.AddConquerorMessageHandlerDelegateInternal<TMessage, TResponse>((m, p, ct) => Task.FromResult(fn(m, p, ct)), null);
    }

    public static IServiceCollection AddConquerorMessageHandlerDelegate<TMessage>(this IServiceCollection services, MessageHandlerSyncFn<TMessage> fn)
        where TMessage : class, IMessage<UnitMessageResponse>
    {
        return services.AddConquerorMessageHandlerDelegateInternal<TMessage, UnitMessageResponse>((m, p, ct) =>
        {
            fn(m, p, ct);
            return Task.FromResult(UnitMessageResponse.Instance);
        }, null);
    }

    public static IServiceCollection AddConquerorMessageHandlerDelegate<TMessage, TResponse>(this IServiceCollection services,
                                                                                             MessageHandlerFn<TMessage, TResponse> fn,
                                                                                             Action<IMessagePipeline<TMessage, TResponse>> configurePipeline)
        where TMessage : class, IMessage<TResponse>
    {
        return services.AddConquerorMessageHandlerDelegateInternal(fn, configurePipeline);
    }

    public static IServiceCollection AddConquerorMessageHandlerDelegate<TMessage>(this IServiceCollection services,
                                                                                  MessageHandlerFn<TMessage> fn,
                                                                                  Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline)
        where TMessage : class, IMessage<UnitMessageResponse>
    {
        return services.AddConquerorMessageHandlerDelegateInternal(async (m, p, ct) =>
        {
            await fn(m, p, ct).ConfigureAwait(false);
            return UnitMessageResponse.Instance;
        }, configurePipeline);
    }

    public static IServiceCollection AddConquerorMessageHandlerDelegate<TMessage, TResponse>(this IServiceCollection services,
                                                                                             MessageHandlerSyncFn<TMessage, TResponse> fn,
                                                                                             Action<IMessagePipeline<TMessage, TResponse>> configurePipeline)
        where TMessage : class, IMessage<TResponse>
    {
        return services.AddConquerorMessageHandlerDelegateInternal((m, p, ct) => Task.FromResult(fn(m, p, ct)), configurePipeline);
    }

    public static IServiceCollection AddConquerorMessageHandlerDelegate<TMessage>(this IServiceCollection services,
                                                                                  MessageHandlerSyncFn<TMessage> fn,
                                                                                  Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline)
        where TMessage : class, IMessage<UnitMessageResponse>
    {
        return services.AddConquerorMessageHandlerDelegateInternal((m, p, ct) =>
        {
            fn(m, p, ct);
            return Task.FromResult(UnitMessageResponse.Instance);
        }, configurePipeline);
    }

    [RequiresUnreferencedCode("Types might be removed")]
    public static IServiceCollection AddConquerorMessageHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        services.AddConqueror();

        var validTypes = assembly.GetTypes()
                                 .Where(t => t is { IsInterface: false, IsAbstract: false, ContainsGenericParameters: false, IsNestedPrivate: false })
                                 .Where(t => t.IsAssignableTo(typeof(IGeneratedMessageHandler)) && !t.IsAssignableTo(typeof(IGeneratedMessageHandlerAdapter)))
                                 .ToList();

        foreach (var messageHandlerType in validTypes)
        {
            var messageHandlerInterfaces = messageHandlerType.GetInterfaces()
                                                             .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageHandler<,>))
                                                             .ToList();

            if (messageHandlerInterfaces.Count > 1)
            {
                continue;
            }

            var messageType = messageHandlerInterfaces[0].GetGenericArguments()[0];
            var responseType = messageHandlerInterfaces[0].GetGenericArguments()[1];

            // TODO: test for correct pipeline configuration
            services.AddConquerorMessageHandlerInternal(ServiceDescriptor.Transient(messageHandlerType, messageHandlerType),
                                                        messageHandlerType,
                                                        messageType,
                                                        responseType,
                                                        null,
                                                        false);
        }

        return services;
    }

    [RequiresUnreferencedCode("Types might be removed")]
    public static IServiceCollection AddConquerorMessageHandlersFromExecutingAssembly(this IServiceCollection services)
    {
        return services.AddConquerorMessageHandlersFromAssembly(Assembly.GetCallingAssembly());
    }

    private static IServiceCollection AddConquerorMessageHandlerInternal<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        ServiceDescriptor serviceDescriptor)
        where THandler : class, IMessageHandler, IGeneratedMessageHandler
    {
        var messageHandlerInterfaces = typeof(THandler).GetInterfaces()
                                                       .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageHandler<,>))
                                                       .ToList();

        if (messageHandlerInterfaces.Count > 1)
        {
            throw new InvalidOperationException($"handler type '{typeof(THandler)}' implements multiple message handler interfaces");
        }

        return services.AddConquerorMessageHandlerInternal(serviceDescriptor,
                                                           typeof(THandler),
                                                           THandler.MessageType(),
                                                           THandler.ResponseType(),
                                                           THandler.CreateConfigurePipeline<THandler>(),
                                                           true);
    }

    private static IServiceCollection AddConquerorMessageHandlerInternal(
        this IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        Type handlerType,
        Type messageType,
        Type responseType,
        Delegate? configurePipeline,
        bool shouldOverwrite)
    {
        if (handlerType.IsInterface || handlerType.IsAbstract)
        {
            throw new InvalidOperationException($"handler type '{handlerType}' must not be an interface or abstract class");
        }

        var existingRegistrations = services.Select(d => d.ImplementationInstance)
                                            .OfType<MessageHandlerRegistration>()
                                            .ToDictionary(r => r.MessageType);

        if (existingRegistrations.TryGetValue(messageType, out var existingRegistration))
        {
            var isSameType = handlerType == existingRegistration.HandlerType;
            var existingIsDelegate = existingRegistration.HandlerType.IsGenericType
                                     && existingRegistration.HandlerType.GetGenericTypeDefinition() == typeof(DelegateMessageHandler<,>);

            if (!isSameType || existingIsDelegate)
            {
                throw new InvalidOperationException($"attempted to register handler type '{handlerType}' for message type '{messageType}', but handler type '{existingRegistration.HandlerType}' is already registered.");
            }
        }
        else
        {
            var registration = new MessageHandlerRegistration(messageType, responseType, handlerType, configurePipeline);
            services.AddSingleton(registration);
        }

        services.AddConqueror();

        if (shouldOverwrite)
        {
            services.Replace(serviceDescriptor);
        }
        else
        {
            services.TryAdd(serviceDescriptor);
        }

        return services;
    }

    private static IServiceCollection AddConquerorMessageHandlerDelegateInternal<TMessage, TResponse>(
        this IServiceCollection services,
        MessageHandlerFn<TMessage, TResponse> fn,
        Action<IMessagePipeline<TMessage, TResponse>>? configurePipeline)
        where TMessage : class, IMessage<TResponse>
    {
        services.AddConqueror();

        services.Replace(new(typeof(DelegateMessageHandler<TMessage, TResponse>), p => new DelegateMessageHandler<TMessage, TResponse>(fn, p), ServiceLifetime.Transient));

        var existingRegistrations = services.Select(d => d.ImplementationInstance)
                                            .OfType<MessageHandlerRegistration>()
                                            .ToDictionary(r => r.MessageType);

        var messageType = typeof(TMessage);
        if (existingRegistrations.TryGetValue(messageType, out var existingRegistration))
        {
            throw new InvalidOperationException($"attempted to register delegate handler for message type '{messageType}', but handler type '{existingRegistration.HandlerType}' is already registered.");
        }

        services.AddSingleton(new MessageHandlerRegistration(typeof(TMessage), typeof(TResponse), typeof(DelegateMessageHandler<TMessage, TResponse>), configurePipeline));

        return services;
    }
}
