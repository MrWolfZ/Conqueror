using System;
using System.Linq;
using System.Reflection;
using Conqueror;
using Conqueror.Eventing;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorEventingServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorEventing(this IServiceCollection services)
    {
        services.TryAddTransient<IConquerorEventDispatcher, EventDispatcher>();
        services.TryAddTransient(typeof(IEventObserver<>), typeof(EventObserverDispatcher<>));
        services.TryAddSingleton<EventPublisherDispatcher>();
        services.TryAddSingleton<EventPublisherRegistry>();
        services.TryAddSingleton<EventTransportClientRegistrar>();
        services.TryAddSingleton<IConquerorEventTransportClientRegistrar>(p => p.GetRequiredService<EventTransportClientRegistrar>());

        services.TryAddDefaultInMemoryEventPublisher();

        return services;
    }

    public static IServiceCollection AddConquerorEventingTypesFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        services.AddConquerorEventing();

        var validTypes = assembly.GetTypes()
                                 .Where(t => t is { IsInterface: false, IsAbstract: false, ContainsGenericParameters: false, IsNestedPrivate: false })
                                 .ToList();

        foreach (var eventObserverType in validTypes.Where(t => t.IsAssignableTo(typeof(IEventObserver))))
        {
            services.AddConquerorEventObserver(eventObserverType, ServiceDescriptor.Transient(eventObserverType, eventObserverType), null);
        }

        foreach (var eventObserverMiddlewareType in validTypes.Where(t => t.GetInterfaces().Any(IsEventObserverMiddlewareInterface)))
        {
            services.AddConquerorEventObserverMiddleware(eventObserverMiddlewareType, ServiceDescriptor.Transient(eventObserverMiddlewareType, eventObserverMiddlewareType));
        }

        foreach (var eventPublisherType in validTypes.Where(t => t.GetInterfaces().Any(IsEventPublisherInterface)))
        {
            services.AddConquerorEventTransportPublisher(eventPublisherType, ServiceDescriptor.Transient(eventPublisherType, eventPublisherType), null);
        }

        foreach (var eventPublisherMiddlewareType in validTypes.Where(t => t.GetInterfaces().Any(IsEventPublisherMiddlewareInterface)))
        {
            services.AddConquerorEventPublisherMiddleware(eventPublisherMiddlewareType, ServiceDescriptor.Transient(eventPublisherMiddlewareType, eventPublisherMiddlewareType));
        }

        return services;

        static bool IsEventObserverMiddlewareInterface(Type i) => i == typeof(IEventObserverMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventObserverMiddleware<>));

        static bool IsEventPublisherInterface(Type i) => i == typeof(IConquerorEventTransportPublisher) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConquerorEventTransportPublisher<>));

        static bool IsEventPublisherMiddlewareInterface(Type i) => i == typeof(IEventPublisherMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventPublisherMiddleware<>));
    }

    public static IServiceCollection AddConquerorEventingTypesFromExecutingAssembly(this IServiceCollection services)
    {
        return services.AddConquerorEventing().AddConquerorEventingTypesFromAssembly(Assembly.GetCallingAssembly());
    }
}
