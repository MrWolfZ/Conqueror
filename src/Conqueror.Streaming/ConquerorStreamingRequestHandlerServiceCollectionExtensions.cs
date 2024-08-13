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

public static class ConquerorStreamingRequestHandlerServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorStreamingRequestHandler<THandler>(this IServiceCollection services,
                                                                                   ServiceLifetime lifetime = ServiceLifetime.Transient)
        where THandler : class, IStreamingRequestHandler
    {
        return services.AddConquerorStreamingRequestHandler(typeof(THandler), new ServiceDescriptor(typeof(THandler), typeof(THandler), lifetime));
    }

    public static IServiceCollection AddConquerorStreamingRequestHandler<THandler>(this IServiceCollection services,
                                                                                   Func<IServiceProvider, THandler> factory,
                                                                                   ServiceLifetime lifetime = ServiceLifetime.Transient)
        where THandler : class, IStreamingRequestHandler
    {
        return services.AddConquerorStreamingRequestHandler(typeof(THandler), new ServiceDescriptor(typeof(THandler), factory, lifetime));
    }

    public static IServiceCollection AddConquerorStreamingRequestHandler<THandler>(this IServiceCollection services,
                                                                                   THandler instance)
        where THandler : class, IStreamingRequestHandler
    {
        return services.AddConquerorStreamingRequestHandler(typeof(THandler), new ServiceDescriptor(typeof(THandler), instance));
    }

    public static IServiceCollection AddConquerorStreamingRequestHandlerDelegate<TRequest, TItem>(this IServiceCollection services,
                                                                                                  Func<TRequest, IServiceProvider, CancellationToken, IAsyncEnumerable<TItem>> handlerFn)
        where TRequest : class
    {
        return services.AddConquerorStreamingRequestHandler(p => new DelegateStreamingRequestHandler<TRequest, TItem>(handlerFn, p));
    }

    public static IServiceCollection AddConquerorStreamingRequestHandlerDelegate<TRequest, TItem>(this IServiceCollection services,
                                                                                                  Func<TRequest, IServiceProvider, CancellationToken, IAsyncEnumerable<TItem>> handlerFn,
                                                                                                  Action<IStreamingRequestPipelineBuilder> configurePipeline)
        where TRequest : class
    {
        return services.AddConquerorStreamingRequestHandler(typeof(DelegateStreamingRequestHandler<TRequest, TItem>),
                                                            ServiceDescriptor.Transient(p => new DelegateStreamingRequestHandler<TRequest, TItem>(handlerFn, p)),
                                                            configurePipeline);
    }

    internal static IServiceCollection AddConquerorStreamingRequestHandler(this IServiceCollection services,
                                                                           Type handlerType,
                                                                           ServiceDescriptor serviceDescriptor,
                                                                           Action<IStreamingRequestPipelineBuilder>? configurePipeline = null)
    {
        services.TryAdd(serviceDescriptor);
        return services.AddConquerorStreamingRequestHandler(handlerType, configurePipeline);
    }

    internal static IServiceCollection AddConquerorStreamingRequestHandler(this IServiceCollection services,
                                                                           Type handlerType,
                                                                           Action<IStreamingRequestPipelineBuilder>? configurePipeline)
    {
        var existingRegistrations = services.Where(d => d.ImplementationInstance is StreamingRequestHandlerRegistration)
                                            .ToDictionary(d => ((StreamingRequestHandlerRegistration)d.ImplementationInstance!).StreamingRequestType);

        foreach (var (requestType, itemType) in handlerType.GetStreamingRequestAndItemTypes())
        {
            if (existingRegistrations.TryGetValue(requestType, out var existingDescriptor))
            {
                if (handlerType == ((StreamingRequestHandlerRegistration)existingDescriptor.ImplementationInstance!).HandlerType)
                {
                    continue;
                }

                services.Remove(existingDescriptor);
            }

            var registration = new StreamingRequestHandlerRegistration(requestType, itemType, handlerType);
            services.AddSingleton(registration);
        }

        var pipelineConfigurationAction = configurePipeline ?? CreatePipelineConfigurationFunction(handlerType);

        services.AddConquerorStreamingRequestClient(handlerType, new InMemoryStreamingRequestTransport(handlerType), pipelineConfigurationAction);

        return services;

        static Action<IStreamingRequestPipelineBuilder>? CreatePipelineConfigurationFunction(Type handlerType)
        {
            if (!handlerType.IsAssignableTo(typeof(IConfigureStreamingRequestPipeline)))
            {
                return null;
            }

            var pipelineConfigurationMethod = handlerType.GetInterfaceMap(typeof(IConfigureStreamingRequestPipeline)).TargetMethods.Single();

            var builderParam = Expression.Parameter(typeof(IStreamingRequestPipelineBuilder));
            var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
            var lambda = Expression.Lambda(body, builderParam).Compile();
            return (Action<IStreamingRequestPipelineBuilder>)lambda;
        }
    }
}
