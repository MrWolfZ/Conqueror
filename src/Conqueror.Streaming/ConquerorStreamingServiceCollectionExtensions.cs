using System;
using System.Linq;
using System.Reflection;
using Conqueror;
using Conqueror.Streaming;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorStreamingServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorStreaming(this IServiceCollection services)
    {
        services.TryAddTransient<IStreamProducerClientFactory, TransientStreamProducerClientFactory>();
        services.TryAddSingleton<StreamProducerClientFactory>();
        services.TryAddSingleton<StreamProducerRegistry>();
        services.TryAddSingleton<IStreamProducerRegistry>(p => p.GetRequiredService<StreamProducerRegistry>());
        services.TryAddSingleton<StreamProducerMiddlewareRegistry>();

        services.AddConquerorContext();

        return services;
    }

    public static IServiceCollection AddConquerorStreamingTypesFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var validTypes = assembly.GetTypes()
                                 .Where(t => t is { IsInterface: false, IsAbstract: false, ContainsGenericParameters: false, IsNestedPrivate: false })
                                 .ToList();

        foreach (var producerType in validTypes.Where(t => t.IsAssignableTo(typeof(IStreamProducer))))
        {
            services.AddConquerorStreamProducer(producerType, ServiceDescriptor.Transient(producerType, producerType));
        }

        foreach (var producerMiddlewareType in validTypes.Where(t => Array.Exists(t.GetInterfaces(), IsStreamProducerMiddlewareInterface)))
        {
            services.AddConquerorStreamProducerMiddleware(producerMiddlewareType, ServiceDescriptor.Transient(producerMiddlewareType, producerMiddlewareType));
        }

        return services;

        static bool IsStreamProducerMiddlewareInterface(Type i) => i == typeof(IStreamProducerMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamProducerMiddleware<>));
    }

    public static IServiceCollection AddConquerorStreamingTypesFromExecutingAssembly(this IServiceCollection services)
    {
        return services.AddConquerorStreamingTypesFromAssembly(Assembly.GetCallingAssembly());
    }
}
