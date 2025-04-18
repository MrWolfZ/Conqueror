using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Messaging;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorMessagingServiceCollectionExtensions
{
    public static IServiceCollection AddMessageHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services)
        where THandler : class, IGeneratedMessageHandler
    {
        return services.AddMessageHandlerInternalGeneric<THandler>(new(typeof(THandler), typeof(THandler), ServiceLifetime.Transient), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddMessageHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime)
        where THandler : class, IGeneratedMessageHandler
    {
        return services.AddMessageHandlerInternalGeneric<THandler>(new(typeof(THandler), typeof(THandler), lifetime), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddMessageHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        Func<IServiceProvider, THandler> factory)
        where THandler : class, IGeneratedMessageHandler
    {
        return services.AddMessageHandlerInternalGeneric<THandler>(new(typeof(THandler), factory, ServiceLifetime.Transient), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddMessageHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        Func<IServiceProvider, THandler> factory,
        ServiceLifetime lifetime)
        where THandler : class, IGeneratedMessageHandler
    {
        return services.AddMessageHandlerInternalGeneric<THandler>(new(typeof(THandler), factory, lifetime), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddMessageHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        THandler instance)
        where THandler : class, IGeneratedMessageHandler
    {
        return services.AddMessageHandlerInternalGeneric<THandler>(new(typeof(THandler), instance), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddMessageHandlerDelegate<TMessage, TResponse>(this IServiceCollection services,
                                                                                    MessageHandlerFn<TMessage, TResponse> fn)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return services.AddMessageHandlerDelegateInternal(fn, null);
    }

    public static IServiceCollection AddMessageHandlerDelegate<TMessage>(this IServiceCollection services,
                                                                         MessageHandlerFn<TMessage> fn)
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
    {
        return services.AddMessageHandlerDelegateInternal(fn, null);
    }

    public static IServiceCollection AddMessageHandlerDelegate<TMessage, TResponse>(this IServiceCollection services,
                                                                                    MessageHandlerSyncFn<TMessage, TResponse> fn)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return services.AddMessageHandlerDelegateInternal<TMessage, TResponse>((m, p, ct) => Task.FromResult(fn(m, p, ct)), null);
    }

    public static IServiceCollection AddMessageHandlerDelegate<TMessage>(this IServiceCollection services,
                                                                         MessageHandlerSyncFn<TMessage> fn)
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
    {
        return services.AddMessageHandlerDelegateInternal<TMessage>((m, p, ct) =>
        {
            fn(m, p, ct);
            return Task.CompletedTask;
        }, null);
    }

    public static IServiceCollection AddMessageHandlerDelegate<TMessage, TResponse>(this IServiceCollection services,
                                                                                    MessageHandlerFn<TMessage, TResponse> fn,
                                                                                    Action<IMessagePipeline<TMessage, TResponse>> configurePipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return services.AddMessageHandlerDelegateInternal(fn, configurePipeline);
    }

    public static IServiceCollection AddMessageHandlerDelegate<TMessage>(this IServiceCollection services,
                                                                         MessageHandlerFn<TMessage> fn,
                                                                         Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline)
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
    {
        return services.AddMessageHandlerDelegateInternal(fn, configurePipeline);
    }

    public static IServiceCollection AddMessageHandlerDelegate<TMessage, TResponse>(this IServiceCollection services,
                                                                                    MessageHandlerSyncFn<TMessage, TResponse> fn,
                                                                                    Action<IMessagePipeline<TMessage, TResponse>> configurePipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return services.AddMessageHandlerDelegateInternal((m, p, ct) => Task.FromResult(fn(m, p, ct)), configurePipeline);
    }

    public static IServiceCollection AddMessageHandlerDelegate<TMessage>(this IServiceCollection services,
                                                                         MessageHandlerSyncFn<TMessage> fn,
                                                                         Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline)
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
    {
        return services.AddMessageHandlerDelegateInternal((m, p, ct) =>
        {
            fn(m, p, ct);
            return Task.CompletedTask;
        }, configurePipeline);
    }

    [RequiresUnreferencedCode("Types might be removed")]
    [RequiresDynamicCode("Types might be removed")]
    public static IServiceCollection AddMessageHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        services.AddConquerorMessaging();

        var validTypes = assembly.GetTypes()
                                 .Where(t => t.IsAssignableTo(typeof(IGeneratedMessageHandler)))
                                 .Where(t => t is { IsInterface: false, IsAbstract: false, ContainsGenericParameters: false, IsNestedPrivate: false, IsNestedFamily: false })
                                 .Where(t => t.DeclaringType?.IsPublic ?? true)
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

            var addHandlerMethod = typeof(ConquerorMessagingServiceCollectionExtensions)
                                   .GetMethod(nameof(AddMessageHandlerInternalGeneric), BindingFlags.NonPublic | BindingFlags.Static)
                                   ?.MakeGenericMethod(messageHandlerType);

            if (addHandlerMethod is null)
            {
                throw new InvalidOperationException($"could not find method '{nameof(ConquerorMessagingServiceCollectionExtensions)}.{nameof(AddMessageHandlerInternalGeneric)}'");
            }

            try
            {
                addHandlerMethod.Invoke(null, [services, ServiceDescriptor.Transient(messageHandlerType, messageHandlerType), false]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        return services;
    }

    [RequiresUnreferencedCode("Types might be removed")]
    [RequiresDynamicCode("Types might be removed")]
    public static IServiceCollection AddMessageHandlersFromExecutingAssembly(this IServiceCollection services)
    {
        return services.AddMessageHandlersFromAssembly(Assembly.GetCallingAssembly());
    }

    internal static void AddConquerorMessaging(this IServiceCollection services)
    {
        services.TryAddTransient<IMessageClients, MessageClients>();
        services.TryAddSingleton<IMessageIdFactory, DefaultMessageIdFactory>();
        services.TryAddSingleton<MessageTransportRegistry>();
        services.TryAddSingleton<IMessageTransportRegistry>(p => p.GetRequiredService<MessageTransportRegistry>());

        services.AddConquerorContext();
    }

    private static IServiceCollection AddMessageHandlerInternalGeneric<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        bool shouldOverwriteRegistration)
        where THandler : class, IGeneratedMessageHandler
    {
        var messageHandlerInterfaces = typeof(THandler).GetInterfaces()
                                                       .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageHandler<,>))
                                                       .ToList();

        if (messageHandlerInterfaces.Count > 1)
        {
            throw new InvalidOperationException($"handler type '{typeof(THandler)}' implements multiple message handler interfaces");
        }

        return THandler.DefaultTypeInjector.CreateWithMessageTypes(new MessageHandlerRegistrationTypeInjectable<THandler>(services,
                                                                                                                          serviceDescriptor,
                                                                                                                          shouldOverwriteRegistration));
    }

    private static IServiceCollection AddMessageHandlerInternal(
        this IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        Type handlerType,
        Type? handlerAdapterType,
        Type messageType,
        Type responseType,
        Delegate? configurePipeline,
        IReadOnlyCollection<IMessageTypesInjector> typeInjectors,
        bool shouldOverwriteRegistration)
    {
        if (handlerType.IsInterface || handlerType.IsAbstract)
        {
            throw new InvalidOperationException($"handler type '{handlerType}' must not be an interface or abstract class");
        }

        var existingRegistration = services.SingleOrDefault(d => d.ImplementationInstance is MessageHandlerRegistration r && r.MessageType == messageType);
        var registration = new MessageHandlerRegistration(messageType, responseType, handlerType, handlerAdapterType, configurePipeline, typeInjectors);

        if (existingRegistration is not null)
        {
            if (shouldOverwriteRegistration)
            {
                services.Remove(existingRegistration);
                services.AddSingleton(registration);
            }
        }
        else
        {
            services.AddSingleton(registration);
        }

        services.AddConquerorMessaging();

        if (shouldOverwriteRegistration)
        {
            services.Replace(serviceDescriptor);
        }
        else
        {
            services.TryAdd(serviceDescriptor);
        }

        return services;
    }

    private static IServiceCollection AddMessageHandlerDelegateInternal<TMessage, TResponse>(
        this IServiceCollection services,
        MessageHandlerFn<TMessage, TResponse> fn,
        Action<IMessagePipeline<TMessage, TResponse>>? configurePipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
    {
        return services.AddMessageHandlerDelegateInternal(
            typeof(TMessage),
            typeof(TResponse),
            typeof(DelegateMessageHandler<TMessage, TResponse>),
            new(typeof(DelegateMessageHandler<TMessage, TResponse>), p => new DelegateMessageHandler<TMessage, TResponse>(fn, p), ServiceLifetime.Transient),
            configurePipeline,
            TMessage.TypeInjectors);
    }

    private static IServiceCollection AddMessageHandlerDelegateInternal<TMessage>(
        this IServiceCollection services,
        MessageHandlerFn<TMessage> fn,
        Action<IMessagePipeline<TMessage, UnitMessageResponse>>? configurePipeline)
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
    {
        return services.AddMessageHandlerDelegateInternal(
            typeof(TMessage),
            typeof(UnitMessageResponse),
            typeof(DelegateMessageHandler<TMessage>),
            new(typeof(DelegateMessageHandler<TMessage>), p => new DelegateMessageHandler<TMessage>(fn, p), ServiceLifetime.Transient),
            configurePipeline,
            TMessage.TypeInjectors);
    }

    private static IServiceCollection AddMessageHandlerDelegateInternal(
        this IServiceCollection services,
        Type messageType,
        Type responseType,
        Type handlerType,
        ServiceDescriptor serviceDescriptor,
        Delegate? configurePipeline,
        IReadOnlyCollection<IMessageTypesInjector> typeInjectors)
    {
        var existingRegistration = services.SingleOrDefault(d => d.ImplementationInstance is MessageHandlerRegistration r && r.MessageType == messageType);

        if (existingRegistration is not null)
        {
            services.Remove(existingRegistration);
        }

        services.AddConquerorMessaging();

        services.Replace(serviceDescriptor);

        services.AddSingleton(new MessageHandlerRegistration(messageType, responseType, handlerType, null, configurePipeline, typeInjectors));

        return services;
    }

    private sealed class MessageHandlerRegistrationTypeInjectable<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        THandler>(
        IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        bool shouldOverwriteRegistration
    ) : IDefaultMessageTypesInjectable<IServiceCollection>
        where THandler : class
    {
        public IServiceCollection WithInjectedTypes<
            TMessage,
            TResponse,
            TGeneratedHandlerInterface,
            TGeneratedHandlerAdapter,
            TPipelineInterface,
            TPipelineAdapter>()
            where TMessage : class, IMessage<TMessage, TResponse>
            where TGeneratedHandlerInterface : class, IGeneratedMessageHandler<TMessage, TResponse, TPipelineInterface>
            where TGeneratedHandlerAdapter : GeneratedMessageHandlerAdapter<TMessage, TResponse>, TGeneratedHandlerInterface, new()
            where TPipelineInterface : class, IMessagePipeline<TMessage, TResponse>
            where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, TResponse>, TPipelineInterface, new()
        {
            Debug.Assert(typeof(THandler).IsAssignableTo(typeof(IMessageHandler<TMessage, TResponse>)),
                         $"handler type '{typeof(THandler)}' should implement {typeof(IMessageHandler<TMessage, TResponse>).Name}");

            return services.AddMessageHandlerInternal(serviceDescriptor,
                                                      typeof(THandler),
                                                      null,
                                                      typeof(TMessage),
                                                      typeof(TResponse),
                                                      MakeConfiguredPipeline(),
                                                      TMessage.TypeInjectors,
                                                      shouldOverwriteRegistration);

            static Action<IMessagePipeline<TMessage, TResponse>>? MakeConfiguredPipeline()
            {
                if (GetConfigurePipelineMethod<TMessage, TResponse, TPipelineInterface>() is not { } methodInfo)
                {
                    return null;
                }

                var configure = (Action<TPipelineInterface>)Delegate.CreateDelegate(typeof(Action<TPipelineInterface>), methodInfo);
                return pipeline => configure(new TPipelineAdapter { Wrapped = pipeline });
            }
        }

        public IServiceCollection WithInjectedTypes<
            TMessage,
            TGeneratedHandlerInterface,
            TGeneratedHandlerAdapter,
            TPipelineInterface,
            TPipelineAdapter>()
            where TMessage : class, IMessage<TMessage, UnitMessageResponse>
            where TGeneratedHandlerInterface : class, IGeneratedMessageHandler<TMessage, TPipelineInterface>
            where TGeneratedHandlerAdapter : GeneratedMessageHandlerAdapter<TMessage>, TGeneratedHandlerInterface, new()
            where TPipelineInterface : class, IMessagePipeline<TMessage, UnitMessageResponse>
            where TPipelineAdapter : GeneratedMessagePipelineAdapter<TMessage, UnitMessageResponse>, TPipelineInterface, new()
        {
            Debug.Assert(typeof(THandler).IsAssignableTo(typeof(IMessageHandler<TMessage>)),
                         $"handler type '{typeof(THandler)}' should implement {typeof(IMessageHandler<TMessage>).Name}");

            var adapterDescriptor = new ServiceDescriptor(typeof(MessageHandlerWithoutResponseAdapter<TMessage>),
                                                          p => new MessageHandlerWithoutResponseAdapter<TMessage>(typeof(THandler), p),
                                                          ServiceLifetime.Transient);

            if (shouldOverwriteRegistration)
            {
                services.Replace(adapterDescriptor);
            }
            else
            {
                services.TryAdd(adapterDescriptor);
            }

            return services.AddMessageHandlerInternal(serviceDescriptor,
                                                      typeof(THandler),
                                                      typeof(MessageHandlerWithoutResponseAdapter<TMessage>),
                                                      typeof(TMessage),
                                                      typeof(UnitMessageResponse),
                                                      MakeConfiguredPipeline(),
                                                      TMessage.TypeInjectors,
                                                      shouldOverwriteRegistration);

            static Action<IMessagePipeline<TMessage, UnitMessageResponse>>? MakeConfiguredPipeline()
            {
                if (GetConfigurePipelineMethod<TMessage, UnitMessageResponse, TPipelineInterface>() is not { } methodInfo)
                {
                    return null;
                }

                var configure = (Action<TPipelineInterface>)Delegate.CreateDelegate(typeof(Action<TPipelineInterface>), methodInfo);
                return pipeline => configure(new TPipelineAdapter { Wrapped = pipeline });
            }
        }

        private static MethodInfo? GetConfigurePipelineMethod<TMessage, TResponse, TPipelineInterface>()
            where TMessage : class, IMessage<TMessage, TResponse>
            where TPipelineInterface : class, IMessagePipeline<TMessage, TResponse>
        {
            const string configurePipelineName = nameof(IGeneratedMessageHandler<TMessage, TResponse, TPipelineInterface>.ConfigurePipeline);

            var methods = typeof(THandler).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                          .Where(m => m.Name == configurePipelineName)
                                          .ToList();

            if (methods.Count == 0)
            {
                return null;
            }

            if (methods.Count > 1)
            {
                throw new InvalidOperationException($"handler type '{typeof(THandler)}' implements '{configurePipelineName}' multiple times");
            }

            var method = methods[0];

            if (method.GetParameters().Length != 1 || method.GetParameters()[0].ParameterType != typeof(TPipelineInterface) || method.ReturnType != typeof(void))
            {
                throw new InvalidOperationException($"handler type '{typeof(THandler)}' does not implement the '{configurePipelineName}' method correctly");
            }

            return method;
        }
    }
}
