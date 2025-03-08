using System;
using System.Linq;
using System.Reflection;
using Conqueror;
using Conqueror.Eventing;
using Conqueror.Eventing.Observing;
using Conqueror.Eventing.Publishing;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorEventingServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorEventing(this IServiceCollection services)
    {
        services.TryAddTransient<IEventDispatcher, EventDispatcher>();
        services.TryAddTransient(typeof(IEventObserver<>), typeof(EventObserverDispatcher<>));
        services.TryAddSingleton<EventPublisherDispatcher>();
        services.TryAddSingleton<EventPublisherRegistry>();
        services.TryAddSingleton<EventTransportRegistry>();
        services.TryAddSingleton<IEventTransportRegistry>(p => p.GetRequiredService<EventTransportRegistry>());
        services.TryAddSingleton<InProcessEventTransportReceiver>();
        services.TryAddSingleton<IEventBroadcastingStrategy>(new SequentialBroadcastingStrategy(new()));
        services.TryAddSingleton<IEventTransportReceiverBroadcaster, EventTransportReceiverBroadcaster>();

        services.TryAddDefaultInMemoryEventPublisher();

        services.AddConquerorContext();

        return services;
    }

    public static IServiceCollection AddConquerorEventingTypesFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        services.AddConquerorEventing();

        var validTypes = assembly.GetTypes()
                                 .Where(t => t is { IsInterface: false, IsAbstract: false, ContainsGenericParameters: false, IsNestedPrivate: false })
                                 .ToList();

        foreach (var eventObserverType in validTypes.Where(t => Array.Exists(t.GetInterfaces(), IsEventObserverInterface)))
        {
            services.TryAddConquerorEventObserver(eventObserverType, ServiceDescriptor.Transient(eventObserverType, eventObserverType));
        }

        foreach (var eventPublisherType in validTypes.Where(t => Array.Exists(t.GetInterfaces(), IsEventPublisherInterface)))
        {
            services.TryAddConquerorEventTransportPublisher(eventPublisherType, ServiceDescriptor.Transient(eventPublisherType, eventPublisherType));
        }

        return services;

        static bool IsEventObserverInterface(Type i) => i == typeof(IEventObserver) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventObserver<>));

        static bool IsEventPublisherInterface(Type i) => i == typeof(IEventTransportPublisher) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventTransportPublisher<>));
    }

    public static IServiceCollection AddConquerorEventingTypesFromExecutingAssembly(this IServiceCollection services)
    {
        return services.AddConquerorEventing().AddConquerorEventingTypesFromAssembly(Assembly.GetCallingAssembly());
    }
}
