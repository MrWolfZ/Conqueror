using System;
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
    public static IServiceCollection AddMessageHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(
        this IServiceCollection services)
        where THandler : class, IMessageHandler
    {
        return services.AddMessageHandlerInternalGeneric<THandler>(new(typeof(THandler), typeof(THandler), ServiceLifetime.Transient), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddMessageHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime)
        where THandler : class, IMessageHandler
    {
        return services.AddMessageHandlerInternalGeneric<THandler>(new(typeof(THandler), typeof(THandler), lifetime), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddMessageHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(
        this IServiceCollection services,
        Func<IServiceProvider, THandler> factory)
        where THandler : class, IMessageHandler
    {
        return services.AddMessageHandlerInternalGeneric<THandler>(new(typeof(THandler), factory, ServiceLifetime.Transient), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddMessageHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(
        this IServiceCollection services,
        Func<IServiceProvider, THandler> factory,
        ServiceLifetime lifetime)
        where THandler : class, IMessageHandler
    {
        return services.AddMessageHandlerInternalGeneric<THandler>(new(typeof(THandler), factory, lifetime), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddMessageHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(
        this IServiceCollection services,
        THandler instance)
        where THandler : class, IMessageHandler
    {
        return services.AddMessageHandlerInternalGeneric<THandler>(new(typeof(THandler), instance), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddMessageHandlerDelegate<TMessage, TResponse, TIHandler>(this IServiceCollection services,
                                                                                               MessageTypes<TMessage, TResponse, TIHandler> messageTypes,
                                                                                               MessageHandlerFn<TMessage, TResponse> fn)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
    {
        return services.AddMessageHandlerDelegateInternal(messageTypes, fn, null);
    }

    public static IServiceCollection AddMessageHandlerDelegate<TMessage, TIHandler>(this IServiceCollection services,
                                                                                    MessageTypes<TMessage, UnitMessageResponse, TIHandler> messageTypes,
                                                                                    MessageHandlerFn<TMessage> fn)
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
        where TIHandler : class, IMessageHandler<TMessage, UnitMessageResponse, TIHandler>
    {
        return services.AddMessageHandlerDelegateInternal(messageTypes, async (m, p, ct) =>
        {
            await fn(m, p, ct).ConfigureAwait(false);
            return UnitMessageResponse.Instance;
        }, null);
    }

    public static IServiceCollection AddMessageHandlerDelegate<TMessage, TResponse, TIHandler>(this IServiceCollection services,
                                                                                               MessageTypes<TMessage, TResponse, TIHandler> messageTypes,
                                                                                               MessageHandlerSyncFn<TMessage, TResponse> fn)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
    {
        return services.AddMessageHandlerDelegateInternal(messageTypes, (m, p, ct) => Task.FromResult(fn(m, p, ct)), null);
    }

    public static IServiceCollection AddMessageHandlerDelegate<TMessage, TIHandler>(this IServiceCollection services,
                                                                                    MessageTypes<TMessage, UnitMessageResponse, TIHandler> messageTypes,
                                                                                    MessageHandlerSyncFn<TMessage> fn)
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
        where TIHandler : class, IMessageHandler<TMessage, UnitMessageResponse, TIHandler>
    {
        return services.AddMessageHandlerDelegateInternal(messageTypes, (m, p, ct) =>
        {
            fn(m, p, ct);
            return Task.FromResult(UnitMessageResponse.Instance);
        }, null);
    }

    public static IServiceCollection AddMessageHandlerDelegate<TMessage, TResponse, TIHandler>(this IServiceCollection services,
                                                                                               MessageTypes<TMessage, TResponse, TIHandler> messageTypes,
                                                                                               MessageHandlerFn<TMessage, TResponse> fn,
                                                                                               Action<IMessagePipeline<TMessage, TResponse>> configurePipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
    {
        return services.AddMessageHandlerDelegateInternal(messageTypes, fn, configurePipeline);
    }

    public static IServiceCollection AddMessageHandlerDelegate<TMessage, TIHandler>(this IServiceCollection services,
                                                                                    MessageTypes<TMessage, UnitMessageResponse, TIHandler> messageTypes,
                                                                                    MessageHandlerFn<TMessage> fn,
                                                                                    Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline)
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
        where TIHandler : class, IMessageHandler<TMessage, UnitMessageResponse, TIHandler>
    {
        return services.AddMessageHandlerDelegateInternal(messageTypes, async (m, p, ct) =>
        {
            await fn(m, p, ct).ConfigureAwait(false);
            return UnitMessageResponse.Instance;
        }, configurePipeline);
    }

    public static IServiceCollection AddMessageHandlerDelegate<TMessage, TResponse, TIHandler>(this IServiceCollection services,
                                                                                               MessageTypes<TMessage, TResponse, TIHandler> messageTypes,
                                                                                               MessageHandlerSyncFn<TMessage, TResponse> fn,
                                                                                               Action<IMessagePipeline<TMessage, TResponse>> configurePipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
    {
        return services.AddMessageHandlerDelegateInternal(messageTypes, (m, p, ct) => Task.FromResult(fn(m, p, ct)), configurePipeline);
    }

    public static IServiceCollection AddMessageHandlerDelegate<TMessage, TIHandler>(this IServiceCollection services,
                                                                                    MessageTypes<TMessage, UnitMessageResponse, TIHandler> messageTypes,
                                                                                    MessageHandlerSyncFn<TMessage> fn,
                                                                                    Action<IMessagePipeline<TMessage, UnitMessageResponse>> configurePipeline)
        where TMessage : class, IMessage<TMessage, UnitMessageResponse>
        where TIHandler : class, IMessageHandler<TMessage, UnitMessageResponse, TIHandler>
    {
        return services.AddMessageHandlerDelegateInternal(messageTypes, (m, p, ct) =>
        {
            fn(m, p, ct);
            return Task.FromResult(UnitMessageResponse.Instance);
        }, configurePipeline);
    }

    public static IServiceCollection AddMessageHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        services.AddConquerorMessaging();

        MessageHandlerTypeServiceRegistry.RunWithRegisteredTypes(new ServiceRegisterable(services, assembly));

        return services;
    }

    internal static IServiceCollection AddConquerorMessaging(this IServiceCollection services)
    {
        services.TryAddTransient<IMessageSenders, MessageSenders>();
        services.TryAddSingleton<IMessageIdFactory, DefaultMessageIdFactory>();
        services.TryAddSingleton<MessageHandlerRegistry>();
        services.TryAddSingleton<IMessageHandlerRegistry>(p => p.GetRequiredService<MessageHandlerRegistry>());

        return services.AddConquerorContext();
    }

    private static IServiceCollection AddMessageHandlerInternalGeneric<THandler>(
        this IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        bool shouldOverwriteRegistration)
        where THandler : class, IMessageHandler
    {
        if (typeof(THandler).IsInterface || typeof(THandler).IsAbstract)
        {
            throw new InvalidOperationException($"handler type '{typeof(THandler)}' must not be an interface or abstract class");
        }

        foreach (var injector in THandler.GetTypeInjectors().OfType<ICoreMessageHandlerTypesInjector>())
        {
            injector.Create(new MessageHandlerRegistrationTypeInjectable(services,
                                                                         serviceDescriptor,
                                                                         shouldOverwriteRegistration));
        }

        return services;
    }

    private static IServiceCollection AddMessageHandlerDelegateInternal<TMessage, TResponse, TIHandler>(
        this IServiceCollection services,
        MessageTypes<TMessage, TResponse, TIHandler> _, // for type inference
        MessageHandlerFn<TMessage, TResponse> fn,
        Action<IMessagePipeline<TMessage, TResponse>>? configurePipeline)
        where TMessage : class, IMessage<TMessage, TResponse>
        where TIHandler : class, IMessageHandler<TMessage, TResponse, TIHandler>
    {
        services.AddConquerorMessaging();

        var invoker = new MessageHandlerInvoker<TMessage, TResponse>(configurePipeline, fn);

        // we do not support configuring the receiver for delegate handlers (yet), since they are mostly
        // designed to support simple testing scenarios, not be used as full-fledged message handlers
        var registration = new MessageHandlerRegistration(typeof(TMessage), typeof(TResponse), null, fn, invoker, [TIHandler.CoreTypesInjector]);

        var existingRegistration = services.SingleOrDefault(d => d.ImplementationInstance is MessageHandlerRegistration r
                                                                 && r.MessageType == typeof(TMessage));

        if (existingRegistration is not null)
        {
            services.Remove(existingRegistration);
        }

        services.AddSingleton(registration);

        return services;
    }

    private sealed class ServiceRegisterable(IServiceCollection services, Assembly assembly) : IMessageHandlerServiceRegisterable
    {
        public void Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>()
            where THandler : class, IMessageHandler
        {
            if (typeof(THandler) is { IsInterface: false, IsAbstract: false, ContainsGenericParameters: false, IsNestedPrivate: false, IsNestedFamily: false }
                && (typeof(THandler).DeclaringType?.IsPublic ?? true)
                && typeof(THandler).Assembly == assembly)
            {
                _ = services.AddMessageHandlerInternalGeneric<THandler>(ServiceDescriptor.Transient<THandler, THandler>(), shouldOverwriteRegistration: false);
            }
        }
    }

    private sealed class MessageHandlerRegistrationTypeInjectable(
        IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        bool shouldOverwriteRegistration
    ) : ICoreMessageHandlerTypesInjectable<IServiceCollection>
    {
        IServiceCollection ICoreMessageHandlerTypesInjectable<IServiceCollection>
            .WithInjectedTypes<TMessage, TResponse, TIHandler, TProxy, TIPipeline, TPipelineProxy, THandler>()
        {
            var invoker = new MessageHandlerInvoker<TMessage, TResponse>(
                p => THandler.ConfigurePipeline(new TPipelineProxy { Wrapped = p }),
                (n, p, ct) => TIHandler.Invoke((TIHandler)p.GetRequiredService(typeof(THandler)), n, ct));

            var registration = new MessageHandlerRegistration(typeof(TMessage), typeof(TResponse), typeof(THandler), null, invoker, THandler.GetTypeInjectors().ToList());

            var existingRegistration = services.SingleOrDefault(d => d.ImplementationInstance is MessageHandlerRegistration r
                                                                     && r.MessageType == typeof(TMessage));

            if (existingRegistration is not null)
            {
                services.Remove(existingRegistration);
            }

            services.AddSingleton(registration);

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
    }
}
