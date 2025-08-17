#pragma warning disable IDE0130 // Namespaces don't match folder structure - it's a convention to place service collection extensions in this namespace

namespace Microsoft.Extensions.DependencyInjection;

using System.Reflection;
using Conqueror;
using Conqueror.Streaming;
using Extensions;

public static class ConquerorStreamingServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorStreaming(this IServiceCollection services)
    {
        services.TryAddTransient<IStreamProducerClientFactory, TransientStreamProducerClientFactory>();
        services.TryAddSingleton<StreamProducerClientFactory>();
        services.TryAddSingleton<StreamProducerRegistry>();
        services.TryAddSingleton<IStreamProducerRegistry>(p => p.GetRequiredService<StreamProducerRegistry>());
        services.TryAddSingleton<StreamProducerMiddlewareRegistry>();

        services.TryAddTransient<StreamConsumerFactory>();
        services.TryAddTransient<IStreamConsumerFactory>(p => p.GetRequiredService<StreamConsumerFactory>());
        services.TryAddSingleton<StreamConsumerMiddlewareRegistry>();

        services.AddConquerorContext();

        return services;
    }

    public static IServiceCollection AddConquerorStreamingTypesFromAssembly(
        this IServiceCollection services,
        Assembly assembly
    )
    {
        var validTypes = assembly
            .GetTypes()
            .Where(t =>
                t is { IsInterface: false, IsAbstract: false, ContainsGenericParameters: false, IsNestedPrivate: false }
            )
            .ToList();

        foreach (var producerType in validTypes.Where(t => t.IsAssignableTo(typeof(IStreamProducer))))
        {
            services.AddConquerorStreamProducer(producerType, ServiceDescriptor.Transient(producerType, producerType));
        }

        foreach (
            var producerMiddlewareType in validTypes.Where(t =>
                Array.Exists(t.GetInterfaces(), IsStreamProducerMiddlewareInterface)
            )
        )
        {
            services.AddConquerorStreamProducerMiddleware(
                producerMiddlewareType,
                ServiceDescriptor.Transient(producerMiddlewareType, producerMiddlewareType)
            );
        }

        foreach (var consumerType in validTypes.Where(t => t.IsAssignableTo(typeof(IStreamConsumer))))
        {
            services.AddConquerorStreamConsumer(
                consumerType,
                ServiceDescriptor.Transient(consumerType, consumerType),
                key: null
            );
        }

        foreach (
            var consumerMiddlewareType in validTypes.Where(t =>
                Array.Exists(t.GetInterfaces(), IsStreamConsumerMiddlewareInterface)
            )
        )
        {
            services.AddConquerorStreamConsumerMiddleware(
                consumerMiddlewareType,
                ServiceDescriptor.Transient(consumerMiddlewareType, consumerMiddlewareType)
            );
        }

        return services;

        static bool IsStreamProducerMiddlewareInterface(Type i)
        {
            return i == typeof(IStreamProducerMiddleware)
                || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamProducerMiddleware<>));
        }

        static bool IsStreamConsumerMiddlewareInterface(Type i)
        {
            return i == typeof(IStreamConsumerMiddleware)
                || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamConsumerMiddleware<>));
        }
    }

    public static IServiceCollection AddConquerorStreamingTypesFromExecutingAssembly(
        this IServiceCollection services
    ) => services.AddConquerorStreamingTypesFromAssembly(Assembly.GetCallingAssembly());
}
