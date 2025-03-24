using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Messaging;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorMessagingServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorMessageHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services)
        where THandler : class, IGeneratedMessageHandler
    {
        return services.AddConquerorMessageHandlerInternal<THandler>(new(typeof(THandler), typeof(THandler), ServiceLifetime.Transient));
    }

    public static IServiceCollection AddConquerorMessageHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime)
        where THandler : class, IGeneratedMessageHandler
    {
        return services.AddConquerorMessageHandlerInternal<THandler>(new(typeof(THandler), typeof(THandler), lifetime));
    }

    public static IServiceCollection AddConquerorMessageHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        Func<IServiceProvider, THandler> factory)
        where THandler : class, IGeneratedMessageHandler
    {
        return services.AddConquerorMessageHandlerInternal<THandler>(new(typeof(THandler), factory, ServiceLifetime.Transient));
    }

    public static IServiceCollection AddConquerorMessageHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        Func<IServiceProvider, THandler> factory,
        ServiceLifetime lifetime)
        where THandler : class, IGeneratedMessageHandler
    {
        return services.AddConquerorMessageHandlerInternal<THandler>(new(typeof(THandler), factory, lifetime));
    }

    public static IServiceCollection AddConquerorMessageHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        THandler instance)
        where THandler : class, IGeneratedMessageHandler
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
        return services.AddConquerorMessageHandlerDelegateInternal(fn, null);
    }

    public static IServiceCollection AddConquerorMessageHandlerDelegate<TMessage, TResponse>(this IServiceCollection services, MessageHandlerSyncFn<TMessage, TResponse> fn)
        where TMessage : class, IMessage<TResponse>
    {
        return services.AddConquerorMessageHandlerDelegateInternal<TMessage, TResponse>((m, p, ct) => Task.FromResult(fn(m, p, ct)), null);
    }

    public static IServiceCollection AddConquerorMessageHandlerDelegate<TMessage>(this IServiceCollection services, MessageHandlerSyncFn<TMessage> fn)
        where TMessage : class, IMessage<UnitMessageResponse>
    {
        return services.AddConquerorMessageHandlerDelegateInternal<TMessage>((m, p, ct) =>
        {
            fn(m, p, ct);
            return Task.CompletedTask;
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
        return services.AddConquerorMessageHandlerDelegateInternal(fn, configurePipeline);
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
            return Task.CompletedTask;
        }, configurePipeline);
    }

    [RequiresUnreferencedCode("Types might be removed")]
    public static IServiceCollection AddConquerorMessageHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        services.AddConquerorMessaging();

        var validTypes = assembly.GetTypes()
                                 .Where(t => t is { IsInterface: false, IsAbstract: false, ContainsGenericParameters: false, IsNestedPrivate: false })
                                 .Where(t => t.IsAssignableTo(typeof(IGeneratedMessageHandler)) && !t.IsAssignableTo(typeof(IGeneratedMessageHandlerAdapter)))
                                 .ToList();

        foreach (var messageHandlerType in validTypes)
        {
            var messageHandlerInterfaces = messageHandlerType.GetInterfaces()
                                                             .Where(t => t.IsGenericType
                                                                         && (t.GetGenericTypeDefinition() == typeof(IMessageHandler<,>)
                                                                             || t.GetGenericTypeDefinition() == typeof(IMessageHandler<>)))
                                                             .ToList();

            if (messageHandlerInterfaces.Count > 1)
            {
                // ignore invalid handlers here instead of throwing to make the code more robust; also, it
                // should not be possible for this exact situation to occur anyway (outside of tests) due to
                // static interface properties causing issues when implementing multiple handler interfaces
                continue;
            }

            var genericArguments = messageHandlerInterfaces[0].GetGenericArguments();
            var messageType = genericArguments[0];
            var responseType = genericArguments.Length > 1 ? genericArguments[1] : typeof(UnitMessageResponse);

            // TODO: test for correct pipeline configuration
            // TODO: test for correct type injectors
            services.AddConquerorMessageHandlerInternal(ServiceDescriptor.Transient(messageHandlerType, messageHandlerType),
                                                        messageHandlerType,
                                                        messageType,
                                                        responseType,
                                                        null,
                                                        [],
                                                        false);
        }

        return services;
    }

    [RequiresUnreferencedCode("Types might be removed")]
    public static IServiceCollection AddConquerorMessageHandlersFromExecutingAssembly(this IServiceCollection services)
    {
        return services.AddConquerorMessageHandlersFromAssembly(Assembly.GetCallingAssembly());
    }

    internal static void AddConquerorMessaging(this IServiceCollection services)
    {
        services.TryAddTransient<IMessageClients, MessageClients>();
        services.TryAddSingleton<MessageTransportRegistry>();
        services.TryAddSingleton<IMessageTransportRegistry>(p => p.GetRequiredService<MessageTransportRegistry>());
    }

    private static IServiceCollection AddConquerorMessageHandlerInternal<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        ServiceDescriptor serviceDescriptor)
        where THandler : class, IGeneratedMessageHandler
    {
        var messageHandlerInterfaces = typeof(THandler).GetInterfaces()
                                                       .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageHandler<,>))
                                                       .ToList();

        if (messageHandlerInterfaces.Count > 1)
        {
            throw new InvalidOperationException($"handler type '{typeof(THandler)}' implements multiple message handler interfaces");
        }

        return THandler.CreateWithMessageTypes(new MessageHandlerRegistrationTypeInjectionFactory<THandler>(services, serviceDescriptor));
    }

    private static IServiceCollection AddConquerorMessageHandlerInternal(
        this IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        Type handlerType,
        Type messageType,
        Type responseType,
        Delegate? configurePipeline,
        IReadOnlyCollection<IMessageTypesInjector> typeInjectors,
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
            var registration = new MessageHandlerRegistration(messageType, responseType, handlerType, configurePipeline, typeInjectors);
            services.AddSingleton(registration);
        }

        services.AddConquerorMessaging();

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
        return services.AddConquerorMessageHandlerDelegateInternal(
            typeof(TMessage),
            typeof(TResponse),
            typeof(DelegateMessageHandler<TMessage, TResponse>),
            new(typeof(DelegateMessageHandler<TMessage, TResponse>), p => new DelegateMessageHandler<TMessage, TResponse>(fn, p), ServiceLifetime.Transient),
            configurePipeline,
            TMessage.TypeInjectors);
    }

    private static IServiceCollection AddConquerorMessageHandlerDelegateInternal<TMessage>(
        this IServiceCollection services,
        MessageHandlerFn<TMessage> fn,
        Action<IMessagePipeline<TMessage, UnitMessageResponse>>? configurePipeline)
        where TMessage : class, IMessage<UnitMessageResponse>
    {
        return services.AddConquerorMessageHandlerDelegateInternal(
            typeof(TMessage),
            typeof(UnitMessageResponse),
            typeof(DelegateMessageHandler<TMessage>),
            new(typeof(DelegateMessageHandler<TMessage>), p => new DelegateMessageHandler<TMessage>(fn, p), ServiceLifetime.Transient),
            configurePipeline,
            TMessage.TypeInjectors);
    }

    private static IServiceCollection AddConquerorMessageHandlerDelegateInternal(
        this IServiceCollection services,
        Type messageType,
        Type responseType,
        Type handlerType,
        ServiceDescriptor serviceDescriptor,
        Delegate? configurePipeline,
        IReadOnlyCollection<IMessageTypesInjector> typeInjectors)
    {
        var existingRegistrations = services.Select(d => d.ImplementationInstance)
                                            .OfType<MessageHandlerRegistration>()
                                            .ToDictionary(r => r.MessageType);

        if (existingRegistrations.TryGetValue(messageType, out var existingRegistration))
        {
            throw new InvalidOperationException($"attempted to register delegate handler for message type '{messageType}', but handler type '{existingRegistration.HandlerType}' is already registered.");
        }

        services.AddConquerorMessaging();

        services.Replace(serviceDescriptor);

        services.AddSingleton(new MessageHandlerRegistration(messageType, responseType, handlerType, configurePipeline, typeInjectors));

        return services;
    }

    private sealed class MessageHandlerRegistrationTypeInjectionFactory<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        THandler>(
        IServiceCollection services,
        ServiceDescriptor serviceDescriptor
    ) : IMessageTypesInjectionFactory<IServiceCollection>
        where THandler : class, IGeneratedMessageHandler
    {
        public IServiceCollection Create<TMessage, TResponse, THandlerInterface, THandlerAdapter, TPipelineInterface, TPipelineAdapter>()
            where TMessage : class, IMessage<TResponse>
            where THandlerInterface : class, IGeneratedMessageHandler<TMessage, TResponse, THandlerInterface, THandlerAdapter, TPipelineInterface, TPipelineAdapter>
            where THandlerAdapter : GeneratedMessageHandlerAdapter<TMessage, TResponse>, THandlerInterface, new()
            where TPipelineInterface : class, IMessagePipeline<TMessage, TResponse>
            where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, TResponse>, TPipelineInterface, new()
        {
            Debug.Assert(typeof(THandler).IsAssignableTo(typeof(THandlerInterface)),
                         $"handler type should implement {nameof(THandlerInterface)}");

            return services.AddConquerorMessageHandlerInternal(serviceDescriptor,
                                                               typeof(THandler),
                                                               typeof(TMessage),
                                                               typeof(TResponse),
                                                               MakeConfiguredPipeline(),
                                                               TMessage.TypeInjectors,
                                                               true);

            static Action<IMessagePipeline<TMessage, TResponse>>? MakeConfiguredPipeline()
            {
                var methodInfo = typeof(THandler).GetMethod(nameof(THandlerInterface.ConfigurePipeline),
                                                            BindingFlags.Public | BindingFlags.Static);

                if (methodInfo == null)
                {
                    return null;
                }

                var configure = (Action<TPipelineInterface>)Delegate.CreateDelegate(typeof(Action<TPipelineInterface>), methodInfo);
                return pipeline => configure(new TPipelineAdapter { Wrapped = pipeline });
            }
        }

        public IServiceCollection Create<TMessage, THandlerInterface, THandlerAdapter, TPipelineInterface, TPipelineAdapter>()
            where TMessage : class, IMessage<UnitMessageResponse>
            where THandlerInterface : class, IGeneratedMessageHandler<TMessage, THandlerInterface, THandlerAdapter, TPipelineInterface, TPipelineAdapter>
            where THandlerAdapter : GeneratedMessageHandlerAdapter<TMessage>, THandlerInterface, new()
            where TPipelineInterface : class, IMessagePipeline<TMessage, UnitMessageResponse>
            where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, UnitMessageResponse>, TPipelineInterface, new()
        {
            Debug.Assert(typeof(THandler).IsAssignableTo(typeof(THandlerInterface)),
                         $"handler type should implement {nameof(THandlerInterface)}");

            return services.AddConquerorMessageHandlerInternal(serviceDescriptor,
                                                               typeof(THandler),
                                                               typeof(TMessage),
                                                               typeof(UnitMessageResponse),
                                                               MakeConfiguredPipeline(),
                                                               TMessage.TypeInjectors,
                                                               true);

            static Action<IMessagePipeline<TMessage, UnitMessageResponse>>? MakeConfiguredPipeline()
            {
                var methodInfo = typeof(THandler).GetMethod(nameof(THandlerInterface.ConfigurePipeline),
                                                            BindingFlags.Public | BindingFlags.Static);

                if (methodInfo == null)
                {
                    return null;
                }

                var configure = (Action<TPipelineInterface>)Delegate.CreateDelegate(typeof(Action<TPipelineInterface>), methodInfo);
                return pipeline => configure(new TPipelineAdapter { Wrapped = pipeline });
            }
        }
    }
}
