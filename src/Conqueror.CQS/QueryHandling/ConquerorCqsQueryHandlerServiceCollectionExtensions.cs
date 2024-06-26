using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.CQS.QueryHandling;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorCqsQueryHandlerServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorQueryHandler<THandler>(this IServiceCollection services,
                                                                        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where THandler : class, IQueryHandler
    {
        return services.AddConquerorQueryHandler(typeof(THandler), new ServiceDescriptor(typeof(THandler), typeof(THandler), lifetime));
    }

    public static IServiceCollection AddConquerorQueryHandler<THandler>(this IServiceCollection services,
                                                                        Func<IServiceProvider, THandler> factory,
                                                                        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where THandler : class, IQueryHandler
    {
        return services.AddConquerorQueryHandler(typeof(THandler), new ServiceDescriptor(typeof(THandler), factory, lifetime));
    }

    public static IServiceCollection AddConquerorQueryHandler<THandler>(this IServiceCollection services,
                                                                        THandler instance)
        where THandler : class, IQueryHandler
    {
        return services.AddConquerorQueryHandler(typeof(THandler), new ServiceDescriptor(typeof(THandler), instance));
    }

    public static IServiceCollection AddConquerorQueryHandlerDelegate<TQuery, TResponse>(this IServiceCollection services,
                                                                                         Func<TQuery, IServiceProvider, CancellationToken, Task<TResponse>> handlerFn)
        where TQuery : class
    {
        return services.AddConquerorQueryHandler(p => new DelegateQueryHandler<TQuery, TResponse>(handlerFn, p));
    }

    public static IServiceCollection AddConquerorQueryHandlerDelegate<TQuery, TResponse>(this IServiceCollection services,
                                                                                         Func<TQuery, IServiceProvider, CancellationToken, Task<TResponse>> handlerFn,
                                                                                         Action<IQueryPipelineBuilder> configurePipeline)
        where TQuery : class
    {
        return services.AddConquerorQueryHandler(typeof(DelegateQueryHandler<TQuery, TResponse>),
                                                 ServiceDescriptor.Transient(p => new DelegateQueryHandler<TQuery, TResponse>(handlerFn, p)),
                                                 configurePipeline);
    }

    internal static IServiceCollection AddConquerorQueryHandler(this IServiceCollection services,
                                                                Type handlerType,
                                                                ServiceDescriptor serviceDescriptor,
                                                                Action<IQueryPipelineBuilder>? configurePipeline = null)
    {
        services.TryAdd(serviceDescriptor);
        return services.AddConquerorQueryHandler(handlerType, configurePipeline);
    }

    internal static IServiceCollection AddConquerorQueryHandler(this IServiceCollection services,
                                                                Type handlerType,
                                                                Action<IQueryPipelineBuilder>? configurePipeline)
    {
        var existingRegistrations = services.Where(d => d.ImplementationInstance is QueryHandlerRegistration)
                                            .ToDictionary(d => ((QueryHandlerRegistration)d.ImplementationInstance!).QueryType);

        foreach (var (queryType, responseType) in handlerType.GetQueryAndResponseTypes())
        {
            if (existingRegistrations.TryGetValue(queryType, out var existingDescriptor))
            {
                if (handlerType == ((QueryHandlerRegistration)existingDescriptor.ImplementationInstance!).HandlerType)
                {
                    continue;
                }

                services.Remove(existingDescriptor);
            }

            var registration = new QueryHandlerRegistration(queryType, responseType, handlerType);
            services.AddSingleton(registration);
        }

        var pipelineConfigurationAction = configurePipeline ?? CreatePipelineConfigurationFunction(handlerType);

        services.AddConquerorQueryClient(handlerType, new InMemoryQueryTransport(handlerType), pipelineConfigurationAction);

        return services;

        static Action<IQueryPipelineBuilder>? CreatePipelineConfigurationFunction(Type handlerType)
        {
            if (!handlerType.IsAssignableTo(typeof(IConfigureQueryPipeline)))
            {
                return null;
            }

            var pipelineConfigurationMethod = handlerType.GetInterfaceMap(typeof(IConfigureQueryPipeline)).TargetMethods.Single();

            var builderParam = Expression.Parameter(typeof(IQueryPipelineBuilder));
            var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
            var lambda = Expression.Lambda(body, builderParam).Compile();
            return (Action<IQueryPipelineBuilder>)lambda;
        }
    }
}
