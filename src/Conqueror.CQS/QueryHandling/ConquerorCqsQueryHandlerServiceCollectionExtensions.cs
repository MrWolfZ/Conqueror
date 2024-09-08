using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
                                                                                         Action<IQueryPipeline<TQuery, TResponse>> configurePipeline)
        where TQuery : class
    {
        return services.AddConquerorQueryHandler(typeof(DelegateQueryHandler<TQuery, TResponse>),
                                                 ServiceDescriptor.Transient(p => new DelegateQueryHandler<TQuery, TResponse>(handlerFn, p)),
                                                 configurePipeline);
    }

    internal static IServiceCollection AddConquerorQueryHandler(this IServiceCollection services,
                                                                Type handlerType,
                                                                ServiceDescriptor serviceDescriptor)
    {
        services.TryAdd(serviceDescriptor);
        return services.AddConquerorQueryHandler(handlerType, (Delegate?)null);
    }

    private static IServiceCollection AddConquerorQueryHandler<TQuery, TResponse>(this IServiceCollection services,
                                                                                  Type handlerType,
                                                                                  ServiceDescriptor serviceDescriptor,
                                                                                  Action<IQueryPipeline<TQuery, TResponse>> configurePipeline)
        where TQuery : class
    {
        services.TryAdd(serviceDescriptor);
        return services.AddConquerorQueryHandler(handlerType, configurePipeline);
    }

    private static IServiceCollection AddConquerorQueryHandler(this IServiceCollection services,
                                                               Type handlerType,
                                                               Delegate? configurePipeline)
    {
        handlerType.ValidateNoInvalidQueryHandlerInterface();

        var addHandlerMethod = typeof(ConquerorCqsQueryHandlerServiceCollectionExtensions).GetMethod(nameof(AddHandler), BindingFlags.NonPublic | BindingFlags.Static);

        if (addHandlerMethod == null)
        {
            throw new InvalidOperationException($"could not find method '{nameof(AddHandler)}'");
        }

        foreach (var (commandType, responseType) in handlerType.GetQueryAndResponseTypes())
        {
            var genericAddMethod = addHandlerMethod.MakeGenericMethod(handlerType, commandType, responseType);

            try
            {
                _ = genericAddMethod.Invoke(null, [services, configurePipeline]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        return services;
    }

    private static IServiceCollection AddHandler<THandler, TQuery, TResponse>(this IServiceCollection services,
                                                                              Action<IQueryPipeline<TQuery, TResponse>>? configurePipeline)
        where THandler : class, IQueryHandler
        where TQuery : class
    {
        var existingRegistrations = services.Where(d => d.ImplementationInstance is QueryHandlerRegistration)
                                            .ToDictionary(d => ((QueryHandlerRegistration)d.ImplementationInstance!).QueryType);

        if (existingRegistrations.TryGetValue(typeof(TQuery), out var existingDescriptor))
        {
            if (typeof(THandler) != ((QueryHandlerRegistration)existingDescriptor.ImplementationInstance!).HandlerType)
            {
                services.Remove(existingDescriptor);
                var registration = new QueryHandlerRegistration(typeof(TQuery), typeof(TResponse), typeof(THandler));
                services.AddSingleton(registration);
            }
        }
        else
        {
            var registration = new QueryHandlerRegistration(typeof(TQuery), typeof(TResponse), typeof(THandler));
            services.AddSingleton(registration);
        }

        var pipelineConfigurationAction = configurePipeline ?? CreatePipelineConfigurationFunction(typeof(THandler));

        services.AddConquerorQueryClient<THandler, TQuery, TResponse>(new InMemoryQueryTransport(typeof(THandler)), pipelineConfigurationAction);

        return services;

        static Action<IQueryPipeline<TQuery, TResponse>> CreatePipelineConfigurationFunction(Type handlerType)
        {
            var pipelineConfigurationMethod = handlerType.GetInterfaceMap(typeof(IQueryHandler<TQuery, TResponse>)).TargetMethods.Single(m => m.Name == nameof(IQueryHandler<TQuery, TResponse>.ConfigurePipeline));

            var builderParam = Expression.Parameter(typeof(IQueryPipeline<TQuery, TResponse>));
            var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
            var lambda = Expression.Lambda(body, builderParam).Compile();
            return (Action<IQueryPipeline<TQuery, TResponse>>)lambda;
        }
    }
}
