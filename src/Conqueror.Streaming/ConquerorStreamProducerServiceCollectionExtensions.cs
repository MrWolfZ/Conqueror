using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Conqueror;
using Conqueror.Streaming;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorStreamProducerServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorStreamProducer<TProducer>(this IServiceCollection services,
                                                                           ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TProducer : class, IStreamProducer
    {
        return services.AddConquerorStreamProducer(typeof(TProducer), new ServiceDescriptor(typeof(TProducer), typeof(TProducer), lifetime));
    }

    public static IServiceCollection AddConquerorStreamProducer<TProducer>(this IServiceCollection services,
                                                                           Func<IServiceProvider, TProducer> factory,
                                                                           ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TProducer : class, IStreamProducer
    {
        return services.AddConquerorStreamProducer(typeof(TProducer), new ServiceDescriptor(typeof(TProducer), factory, lifetime));
    }

    public static IServiceCollection AddConquerorStreamProducer<TProducer>(this IServiceCollection services,
                                                                           TProducer instance)
        where TProducer : class, IStreamProducer
    {
        return services.AddConquerorStreamProducer(typeof(TProducer), new ServiceDescriptor(typeof(TProducer), instance));
    }

    public static IServiceCollection AddConquerorStreamProducerDelegate<TRequest, TItem>(this IServiceCollection services,
                                                                                         Func<TRequest, IServiceProvider, CancellationToken, IAsyncEnumerable<TItem>> producerFn)
        where TRequest : class
    {
        return services.AddConquerorStreamProducer(p => new DelegateStreamProducer<TRequest, TItem>(producerFn, p));
    }

    public static IServiceCollection AddConquerorStreamProducerDelegate<TRequest, TItem>(this IServiceCollection services,
                                                                                         Func<TRequest, IServiceProvider, CancellationToken, IAsyncEnumerable<TItem>> producerFn,
                                                                                         Action<IStreamProducerPipelineBuilder> configurePipeline)
        where TRequest : class
    {
        return services.AddConquerorStreamProducer(typeof(DelegateStreamProducer<TRequest, TItem>),
                                                   ServiceDescriptor.Transient(p => new DelegateStreamProducer<TRequest, TItem>(producerFn, p)),
                                                   configurePipeline);
    }

    internal static IServiceCollection AddConquerorStreamProducer(this IServiceCollection services,
                                                                  Type producerType,
                                                                  ServiceDescriptor serviceDescriptor,
                                                                  Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
    {
        services.TryAdd(serviceDescriptor);
        return services.AddConquerorStreamProducer(producerType, configurePipeline);
    }

    internal static IServiceCollection AddConquerorStreamProducer(this IServiceCollection services,
                                                                  Type producerType,
                                                                  Action<IStreamProducerPipelineBuilder>? configurePipeline)
    {
        var existingRegistrations = services.Where(d => d.ImplementationInstance is StreamProducerRegistration)
                                            .ToDictionary(d => ((StreamProducerRegistration)d.ImplementationInstance!).RequestType);

        foreach (var (requestType, itemType) in producerType.GetStreamProducerRequestAndItemTypes())
        {
            if (existingRegistrations.TryGetValue(requestType, out var existingDescriptor))
            {
                if (producerType == ((StreamProducerRegistration)existingDescriptor.ImplementationInstance!).ProducerType)
                {
                    continue;
                }

                services.Remove(existingDescriptor);
            }

            var registration = new StreamProducerRegistration(requestType, itemType, producerType);
            services.AddSingleton(registration);
        }

        var pipelineConfigurationAction = configurePipeline ?? CreatePipelineConfigurationFunction(producerType);

        services.AddConquerorStreamProducerClient(producerType, new InMemoryStreamProducerTransport(producerType), pipelineConfigurationAction);

        return services;

        static Action<IStreamProducerPipelineBuilder>? CreatePipelineConfigurationFunction(Type producerType)
        {
            if (!producerType.IsAssignableTo(typeof(IConfigureStreamProducerPipeline)))
            {
                return null;
            }

            var pipelineConfigurationMethod = producerType.GetInterfaceMap(typeof(IConfigureStreamProducerPipeline)).TargetMethods.Single();

            var builderParam = Expression.Parameter(typeof(IStreamProducerPipelineBuilder));
            var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
            var lambda = Expression.Lambda(body, builderParam).Compile();
            return (Action<IStreamProducerPipelineBuilder>)lambda;
        }
    }
}
