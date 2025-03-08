using System;
using System.Linq;
using Conqueror;
using Conqueror.Eventing.Publishing;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorEventingTransportPublisherServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorEventTransportPublisher<TPublisher>(this IServiceCollection services)
        where TPublisher : class, IEventTransportPublisher
    {
        return services.AddConquerorEventTransportPublisher<TPublisher>(ServiceLifetime.Transient);
    }

    public static IServiceCollection AddConquerorEventTransportPublisher<TPublisher>(this IServiceCollection services, ServiceLifetime lifetime)
        where TPublisher : class, IEventTransportPublisher
    {
        return services.AddConquerorEventTransportPublisher(typeof(TPublisher), new(typeof(TPublisher), typeof(TPublisher), lifetime));
    }

    public static IServiceCollection AddConquerorEventTransportPublisher<TPublisher>(this IServiceCollection services,
                                                                                     Func<IServiceProvider, TPublisher> factory)
        where TPublisher : class, IEventTransportPublisher
    {
        return services.AddConquerorEventTransportPublisher(factory, ServiceLifetime.Transient);
    }

    public static IServiceCollection AddConquerorEventTransportPublisher<TPublisher>(this IServiceCollection services,
                                                                                     Func<IServiceProvider, TPublisher> factory,
                                                                                     ServiceLifetime lifetime)
        where TPublisher : class, IEventTransportPublisher
    {
        return services.AddConquerorEventTransportPublisher(typeof(TPublisher), new(typeof(TPublisher), factory, lifetime));
    }

    public static IServiceCollection AddConquerorEventTransportPublisher<TPublisher>(this IServiceCollection services, TPublisher instance)
        where TPublisher : class, IEventTransportPublisher
    {
        return services.AddConquerorEventTransportPublisher(typeof(TPublisher), new(typeof(TPublisher), instance));
    }

    internal static void TryAddDefaultInMemoryEventPublisher(this IServiceCollection services)
    {
        var inMemoryPublisherIsRegistered = services.Select(d => d.ImplementationInstance)
                                                    .OfType<EventTransportPublisherRegistration>()
                                                    .Any(r => r.PublisherType == typeof(InProcessEventPublisher));

        if (!inMemoryPublisherIsRegistered)
        {
            services.AddConquerorEventTransportPublisher<InProcessEventPublisher>(ServiceLifetime.Singleton);
        }
    }

    internal static void TryAddConquerorEventTransportPublisher(this IServiceCollection services,
                                                                Type publisherType,
                                                                ServiceDescriptor serviceDescriptor)
    {
        services.TryAdd(serviceDescriptor);
        services.AddConquerorEventTransportPublisher(publisherType);
    }

    private static IServiceCollection AddConquerorEventTransportPublisher(this IServiceCollection services,
                                                                          Type publisherType,
                                                                          ServiceDescriptor serviceDescriptor)
    {
        services.Replace(serviceDescriptor);
        return services.AddConquerorEventTransportPublisher(publisherType);
    }

    private static IServiceCollection AddConquerorEventTransportPublisher(this IServiceCollection services,
                                                                          Type publisherType)
    {
        publisherType.ValidateNoInvalidEventPublisherInterface();

        var existingRegistrations = services.Where(d => d.ImplementationInstance is EventTransportPublisherRegistration)
                                            .GroupBy(d => ((EventTransportPublisherRegistration)d.ImplementationInstance!).PublisherType)
                                            .ToDictionary(g => g.Key, g => g.ToList());

        if (existingRegistrations.TryGetValue(publisherType, out var regs))
        {
            foreach (var reg in regs)
            {
                services.Remove(reg);
            }
        }

        foreach (var attributeType in publisherType.GetPublisherConfigurationAttributeTypes())
        {
            var registration = new EventTransportPublisherRegistration(publisherType, attributeType);
            services.AddSingleton(registration);
        }

        // add conqueror services after the registration in order to prevent infinite loop
        // when registering in-memory publisher
        services.AddConquerorEventing();

        return services;
    }
}
