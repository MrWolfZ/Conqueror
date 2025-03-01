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
        where TPublisher : class, IConquerorEventTransportPublisher
    {
        return services.AddConquerorEventTransportPublisher<TPublisher>(ServiceLifetime.Transient);
    }

    public static IServiceCollection AddConquerorEventTransportPublisher<TPublisher>(this IServiceCollection services, ServiceLifetime lifetime)
        where TPublisher : class, IConquerorEventTransportPublisher
    {
        return services.AddConquerorEventTransportPublisher(typeof(TPublisher), new(typeof(TPublisher), typeof(TPublisher), lifetime), null);
    }

    public static IServiceCollection AddConquerorEventTransportPublisher<TPublisher>(this IServiceCollection services,
                                                                                     Action<IEventPublisherPipelineBuilder> configurePipeline)
        where TPublisher : class, IConquerorEventTransportPublisher
    {
        return services.AddConquerorEventTransportPublisher<TPublisher>(configurePipeline, ServiceLifetime.Transient);
    }

    public static IServiceCollection AddConquerorEventTransportPublisher<TPublisher>(this IServiceCollection services,
                                                                                     Action<IEventPublisherPipelineBuilder> configurePipeline,
                                                                                     ServiceLifetime lifetime)
        where TPublisher : class, IConquerorEventTransportPublisher
    {
        return services.AddConquerorEventTransportPublisher(typeof(TPublisher), new(typeof(TPublisher), typeof(TPublisher), lifetime), configurePipeline);
    }

    public static IServiceCollection AddConquerorEventTransportPublisher<TPublisher>(this IServiceCollection services,
                                                                                     Func<IServiceProvider, TPublisher> factory)
        where TPublisher : class, IConquerorEventTransportPublisher
    {
        return services.AddConquerorEventTransportPublisher(factory, ServiceLifetime.Transient);
    }

    public static IServiceCollection AddConquerorEventTransportPublisher<TPublisher>(this IServiceCollection services,
                                                                                     Func<IServiceProvider, TPublisher> factory,
                                                                                     ServiceLifetime lifetime)
        where TPublisher : class, IConquerorEventTransportPublisher
    {
        return services.AddConquerorEventTransportPublisher(typeof(TPublisher), new(typeof(TPublisher), factory, lifetime), null);
    }

    public static IServiceCollection AddConquerorEventTransportPublisher<TPublisher>(this IServiceCollection services,
                                                                                     Func<IServiceProvider, TPublisher> factory,
                                                                                     Action<IEventPublisherPipelineBuilder> configurePipeline)
        where TPublisher : class, IConquerorEventTransportPublisher
    {
        return services.AddConquerorEventTransportPublisher(factory, configurePipeline, ServiceLifetime.Transient);
    }

    public static IServiceCollection AddConquerorEventTransportPublisher<TPublisher>(this IServiceCollection services,
                                                                                     Func<IServiceProvider, TPublisher> factory,
                                                                                     Action<IEventPublisherPipelineBuilder> configurePipeline,
                                                                                     ServiceLifetime lifetime)
        where TPublisher : class, IConquerorEventTransportPublisher
    {
        return services.AddConquerorEventTransportPublisher(typeof(TPublisher), new(typeof(TPublisher), factory, lifetime), configurePipeline);
    }

    public static IServiceCollection AddConquerorEventTransportPublisher<TPublisher>(this IServiceCollection services, TPublisher instance)
        where TPublisher : class, IConquerorEventTransportPublisher
    {
        return services.AddConquerorEventTransportPublisher(typeof(TPublisher), new(typeof(TPublisher), instance), null);
    }

    public static IServiceCollection AddConquerorEventTransportPublisher<TPublisher>(this IServiceCollection services,
                                                                                     TPublisher instance,
                                                                                     Action<IEventPublisherPipelineBuilder> configurePipeline)
        where TPublisher : class, IConquerorEventTransportPublisher
    {
        return services.AddConquerorEventTransportPublisher(typeof(TPublisher), new(typeof(TPublisher), instance), configurePipeline);
    }

    public static IServiceCollection AddConquerorInMemoryEventPublisher(this IServiceCollection services)
    {
        return services.AddConquerorEventTransportPublisher(p => new InMemoryEventPublisher(p.GetRequiredService<IConquerorEventTransportClientRegistrar>(), _ => { }),
                                                            ServiceLifetime.Singleton);
    }

    public static IServiceCollection AddConquerorInMemoryEventPublisher(this IServiceCollection services,
                                                                        Action<IEventPublisherPipelineBuilder> configurePipeline)
    {
        return services.AddConquerorEventTransportPublisher(p => new InMemoryEventPublisher(p.GetRequiredService<IConquerorEventTransportClientRegistrar>(), _ => { }),
                                                            configurePipeline,
                                                            ServiceLifetime.Singleton);
    }

    public static IServiceCollection AddConquerorInMemoryEventPublisher(this IServiceCollection services,
                                                                        Action<IConquerorInMemoryEventPublishingStrategyBuilder> configureStrategy)
    {
        return services.AddConquerorEventTransportPublisher(p => new InMemoryEventPublisher(p.GetRequiredService<IConquerorEventTransportClientRegistrar>(), configureStrategy),
                                                            ServiceLifetime.Singleton);
    }

    public static IServiceCollection AddConquerorInMemoryEventPublisher(this IServiceCollection services,
                                                                        Action<IEventPublisherPipelineBuilder> configurePipeline,
                                                                        Action<IConquerorInMemoryEventPublishingStrategyBuilder> configureStrategy)
    {
        return services.AddConquerorEventTransportPublisher(p => new InMemoryEventPublisher(p.GetRequiredService<IConquerorEventTransportClientRegistrar>(), configureStrategy),
                                                            configurePipeline,
                                                            ServiceLifetime.Singleton);
    }

    internal static void TryAddDefaultInMemoryEventPublisher(this IServiceCollection services)
    {
        var inMemoryPublisherIsRegistered = services.Select(d => d.ImplementationInstance)
                                                    .OfType<EventPublisherRegistration>()
                                                    .Any(r => r.PublisherType == typeof(InMemoryEventPublisher));

        if (!inMemoryPublisherIsRegistered)
        {
            services.AddConquerorInMemoryEventPublisher();
        }
    }

    internal static IServiceCollection AddConquerorEventTransportPublisher(this IServiceCollection services,
                                                                           Type publisherType,
                                                                           ServiceDescriptor serviceDescriptor,
                                                                           Action<IEventPublisherPipelineBuilder>? configurePipeline)
    {
        services.Replace(serviceDescriptor);
        return services.AddConquerorEventTransportPublisher(publisherType, configurePipeline);
    }

    internal static IServiceCollection AddConquerorEventTransportPublisher(this IServiceCollection services,
                                                                           Type publisherType,
                                                                           Action<IEventPublisherPipelineBuilder>? configurePipeline)
    {
        publisherType.ValidateNoInvalidEventPublisherInterface();

        var existingRegistrations = services.Where(d => d.ImplementationInstance is EventPublisherRegistration)
                                            .ToDictionary(d => ((EventPublisherRegistration)d.ImplementationInstance!).PublisherType);

        if (existingRegistrations.TryGetValue(publisherType, out var existingRegistration))
        {
            services.Remove(existingRegistration);
        }

        var configurationAttributeType = publisherType.GetPublisherConfigurationAttributeType();
        var registration = new EventPublisherRegistration(publisherType, configurationAttributeType, configurePipeline);
        services.AddSingleton(registration);

        // add conqueror services after the registration in order to prevent infinite loop
        // when registering in-memory publisher
        services.AddConquerorEventing();

        return services;
    }

    internal static bool IsEventTransportPublisherRegistered(this IServiceCollection services, Type publisherType)
    {
        return services.Any(d => d.ImplementationInstance is EventPublisherRegistration r && r.PublisherType == publisherType);
    }
}
