using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Eventing;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorEventingServiceCollectionExtensions
{
    public static IServiceCollection AddEventNotificationHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services)
        where THandler : class, IGeneratedEventNotificationHandler
    {
        return services.AddEventNotificationHandlerInternalGeneric<THandler>(new(typeof(THandler), typeof(THandler), ServiceLifetime.Transient), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddEventNotificationHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime)
        where THandler : class, IGeneratedEventNotificationHandler
    {
        return services.AddEventNotificationHandlerInternalGeneric<THandler>(new(typeof(THandler), typeof(THandler), lifetime), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddEventNotificationHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        Func<IServiceProvider, THandler> factory)
        where THandler : class, IGeneratedEventNotificationHandler
    {
        return services.AddEventNotificationHandlerInternalGeneric<THandler>(new(typeof(THandler), factory, ServiceLifetime.Transient), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddEventNotificationHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        Func<IServiceProvider, THandler> factory,
        ServiceLifetime lifetime)
        where THandler : class, IGeneratedEventNotificationHandler
    {
        return services.AddEventNotificationHandlerInternalGeneric<THandler>(new(typeof(THandler), factory, lifetime), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddEventNotificationHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        THandler instance)
        where THandler : class, IGeneratedEventNotificationHandler
    {
        return services.AddEventNotificationHandlerInternalGeneric<THandler>(new(typeof(THandler), instance), shouldOverwriteRegistration: true);
    }

    public static IServiceCollection AddEventNotificationHandlerDelegate<TEventNotification>(this IServiceCollection services,
                                                                                             EventNotificationHandlerFn<TEventNotification> fn)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        return services.AddEventNotificationHandlerDelegateInternal(fn, null);
    }

    public static IServiceCollection AddEventNotificationHandlerDelegate<TEventNotification>(this IServiceCollection services,
                                                                                             EventNotificationTypes<TEventNotification> notificationTypes,
                                                                                             EventNotificationHandlerFn<TEventNotification> fn)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        return services.AddEventNotificationHandlerDelegate(fn);
    }

    public static IServiceCollection AddEventNotificationHandlerDelegate<TEventNotification>(this IServiceCollection services,
                                                                                             EventNotificationHandlerSyncFn<TEventNotification> fn)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        return services.AddEventNotificationHandlerDelegateInternal<TEventNotification>((m, p, ct) =>
        {
            fn(m, p, ct);
            return Task.CompletedTask;
        }, null);
    }

    public static IServiceCollection AddEventNotificationHandlerDelegate<TEventNotification>(this IServiceCollection services,
                                                                                             EventNotificationTypes<TEventNotification> notificationTypes,
                                                                                             EventNotificationHandlerSyncFn<TEventNotification> fn)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        return services.AddEventNotificationHandlerDelegate(fn);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static IServiceCollection AddEventNotificationHandlerDelegate<TEventNotification>(this IServiceCollection services,
                                                                                             EventNotificationHandlerFn<TEventNotification> fn,
                                                                                             Action<IEventNotificationPipeline<TEventNotification>> configurePipeline)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        return services.AddEventNotificationHandlerDelegateInternal(fn, configurePipeline);
    }

    public static IServiceCollection AddEventNotificationHandlerDelegate<TEventNotification>(this IServiceCollection services,
                                                                                             EventNotificationTypes<TEventNotification> notificationTypes,
                                                                                             EventNotificationHandlerFn<TEventNotification> fn,
                                                                                             Action<IEventNotificationPipeline<TEventNotification>> configurePipeline)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        return services.AddEventNotificationHandlerDelegate(fn, configurePipeline);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static IServiceCollection AddEventNotificationHandlerDelegate<TEventNotification>(this IServiceCollection services,
                                                                                             EventNotificationHandlerSyncFn<TEventNotification> fn,
                                                                                             Action<IEventNotificationPipeline<TEventNotification>> configurePipeline)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        return services.AddEventNotificationHandlerDelegateInternal((m, p, ct) =>
        {
            fn(m, p, ct);
            return Task.CompletedTask;
        }, configurePipeline);
    }

    public static IServiceCollection AddEventNotificationHandlerDelegate<TEventNotification>(this IServiceCollection services,
                                                                                             EventNotificationTypes<TEventNotification> notificationTypes,
                                                                                             EventNotificationHandlerSyncFn<TEventNotification> fn,
                                                                                             Action<IEventNotificationPipeline<TEventNotification>> configurePipeline)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        return services.AddEventNotificationHandlerDelegate(fn, configurePipeline);
    }

    [RequiresUnreferencedCode("Types might be removed")]
    [RequiresDynamicCode("Types might be removed")]
    public static IServiceCollection AddEventNotificationHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        services.AddConquerorEventing();

        var validTypes = assembly.GetTypes()
                                 .Where(t => t.IsAssignableTo(typeof(IGeneratedEventNotificationHandler)))
                                 .Where(t => t is { IsInterface: false, IsAbstract: false, ContainsGenericParameters: false, IsNestedPrivate: false, IsNestedFamily: false })
                                 .Where(t => t.DeclaringType?.IsPublic ?? true)
                                 .ToList();

        foreach (var notificationHandlerType in validTypes)
        {
            var addHandlerMethod = typeof(ConquerorEventingServiceCollectionExtensions)
                                   .GetMethod(nameof(AddEventNotificationHandlerInternalGeneric), BindingFlags.NonPublic | BindingFlags.Static)
                                   ?.MakeGenericMethod(notificationHandlerType);

            if (addHandlerMethod is null)
            {
                throw new InvalidOperationException($"could not find method '{nameof(ConquerorMessagingServiceCollectionExtensions)}.{nameof(AddEventNotificationHandlerInternalGeneric)}'");
            }

            try
            {
                addHandlerMethod.Invoke(null, [services, ServiceDescriptor.Transient(notificationHandlerType, notificationHandlerType), false]);
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
    public static IServiceCollection AddEventNotificationHandlersFromExecutingAssembly(this IServiceCollection services)
    {
        return services.AddEventNotificationHandlersFromAssembly(Assembly.GetCallingAssembly());
    }

    internal static IServiceCollection AddConquerorEventing(this IServiceCollection services)
    {
        services.TryAddTransient<IEventNotificationPublishers, EventNotificationPublishers>();
        services.TryAddSingleton<IEventNotificationIdFactory, DefaultEventNotificationIdFactory>();
        services.TryAddSingleton<EventNotificationTransportRegistry>();
        services.TryAddSingleton<IEventNotificationTransportRegistry>(p => p.GetRequiredService<EventNotificationTransportRegistry>());
        services.TryAddSingleton<InProcessEventNotificationReceiver>();

        return services.AddConquerorContext();
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
                                  Justification = "we know that both the handler and notification type are reference types and therefore can use shared code")]
    private static IServiceCollection AddEventNotificationHandlerInternalGeneric<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        THandler>(
        this IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        bool shouldOverwriteRegistration)
        where THandler : class, IGeneratedEventNotificationHandler
    {
        var notificationHandlerInterfaces = typeof(THandler).GetInterfaces()
                                                            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEventNotificationHandler<>))
                                                            .ToList();

        foreach (var notificationHandlerInterface in notificationHandlerInterfaces)
        {
            var notificationType = notificationHandlerInterface.GetGenericArguments()[0];
            var addHandlerMethod = typeof(ConquerorEventingServiceCollectionExtensions)
                                   .GetMethod(nameof(AddEventNotificationHandlerInternalForEventNotificationType), BindingFlags.NonPublic | BindingFlags.Static)
                                   ?.MakeGenericMethod(typeof(THandler), notificationType);

            if (addHandlerMethod is null)
            {
                throw new InvalidOperationException($"could not find method '{nameof(ConquerorMessagingServiceCollectionExtensions)}.{nameof(AddEventNotificationHandlerInternalForEventNotificationType)}'");
            }

            try
            {
                addHandlerMethod.Invoke(null, [services, serviceDescriptor, shouldOverwriteRegistration]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        return services;
    }

    private static IServiceCollection AddEventNotificationHandlerInternalForEventNotificationType<THandler, TEventNotification>(
        this IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        bool shouldOverwriteRegistration)
        where THandler : class, IGeneratedEventNotificationHandler<TEventNotification>
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        return THandler.DefaultTypeInjector.CreateWithEventNotificationTypes(new EventNotificationHandlerRegistrationTypeInjectable<THandler>(services,
                                                                                                                                              serviceDescriptor,
                                                                                                                                              shouldOverwriteRegistration));
    }

    private static IServiceCollection AddEventNotificationHandlerInternal<TEventNotification>(
        this IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        Type handlerType,
        Action<IEventNotificationPipeline<TEventNotification>>? configurePipeline,
        Func<IEventNotificationReceiver, Task> configureReceiver,
        IReadOnlyCollection<IEventNotificationTypesInjector> typeInjectors,
        bool shouldOverwriteRegistration)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        if (handlerType.IsInterface || handlerType.IsAbstract)
        {
            throw new InvalidOperationException($"handler type '{handlerType}' must not be an interface or abstract class");
        }

        var existingRegistration = services.SingleOrDefault(d => d.ImplementationInstance is EventNotificationHandlerRegistration r
                                                                 && r.EventNotificationType == typeof(TEventNotification)
                                                                 && r.HandlerType == handlerType);

        var invoker = new EventNotificationHandlerInvoker<TEventNotification>(configurePipeline,
                                                                              (n, p, ct) => ((IEventNotificationHandler<TEventNotification>)p.GetRequiredService(handlerType)).Handle(n, ct));

        var registration = new EventNotificationHandlerRegistration(typeof(TEventNotification), handlerType, null, invoker, configureReceiver, typeInjectors);

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

        services.AddConquerorEventing();

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

    private static IServiceCollection AddEventNotificationHandlerDelegateInternal<TEventNotification>(
        this IServiceCollection services,
        EventNotificationHandlerFn<TEventNotification> fn,
        Action<IEventNotificationPipeline<TEventNotification>>? configurePipeline)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        services.AddConquerorEventing();

        var invoker = new EventNotificationHandlerInvoker<TEventNotification>(configurePipeline, fn);

        // we do not support configuring the receiver for delegate handlers (yet), since they are mostly
        // designed to support simple testing scenarios, not be used as full-fledged event notification handlers
        services.AddSingleton(new EventNotificationHandlerRegistration(typeof(TEventNotification), null, fn, invoker, null, TEventNotification.TypeInjectors));

        return services;
    }

    private sealed class EventNotificationHandlerRegistrationTypeInjectable<THandler>(
        IServiceCollection services,
        ServiceDescriptor serviceDescriptor,
        bool shouldOverwriteRegistration
    ) : IDefaultEventNotificationTypesInjectable<IServiceCollection>
        where THandler : class, IGeneratedEventNotificationHandler
    {
        public IServiceCollection WithInjectedTypes<
            TEventNotification,
            TGeneratedHandlerInterface,
            TGeneratedHandlerAdapter>()
            where TEventNotification : class, IEventNotification<TEventNotification>
            where TGeneratedHandlerInterface : class, IGeneratedEventNotificationHandler<TEventNotification>
            where TGeneratedHandlerAdapter : GeneratedEventNotificationHandlerAdapter<TEventNotification>, TGeneratedHandlerInterface, new()
        {
            Debug.Assert(typeof(THandler).IsAssignableTo(typeof(IEventNotificationHandler<TEventNotification>)),
                         $"handler type '{typeof(THandler)}' should implement {typeof(IEventNotificationHandler<TEventNotification>).Name}");

            return services.AddEventNotificationHandlerInternal<TEventNotification>(serviceDescriptor,
                                                                                    typeof(THandler),
                                                                                    THandler.ConfigurePipeline,
                                                                                    THandler.ConfigureReceiver,
                                                                                    TEventNotification.TypeInjectors,
                                                                                    shouldOverwriteRegistration);
        }
    }
}
