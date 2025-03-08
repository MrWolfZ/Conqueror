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
    public static IServiceCollection AddConquerorQueryHandler<THandler>(this IServiceCollection services)
        where THandler : class, IQueryHandler
    {
        return services.AddConquerorQueryHandler<THandler>(ServiceLifetime.Transient);
    }

    public static IServiceCollection AddConquerorQueryHandler<THandler>(this IServiceCollection services, ServiceLifetime lifetime)
        where THandler : class, IQueryHandler
    {
        return services.AddConquerorQueryHandler(typeof(THandler), ServiceDescriptor.Describe(typeof(THandler), typeof(THandler), lifetime));
    }

    public static IServiceCollection AddConquerorQueryHandler<THandler>(this IServiceCollection services,
                                                                        Func<IServiceProvider, THandler> factory)
        where THandler : class, IQueryHandler
    {
        return services.AddConquerorQueryHandler(factory, ServiceLifetime.Transient);
    }

    public static IServiceCollection AddConquerorQueryHandler<THandler>(this IServiceCollection services,
                                                                        Func<IServiceProvider, THandler> factory,
                                                                        ServiceLifetime lifetime)
        where THandler : class, IQueryHandler
    {
        return services.AddConquerorQueryHandler(typeof(THandler), ServiceDescriptor.Describe(typeof(THandler), factory, lifetime));
    }

    public static IServiceCollection AddConquerorQueryHandler<THandler>(this IServiceCollection services,
                                                                        THandler instance)
        where THandler : class, IQueryHandler
    {
        return services.AddConquerorQueryHandler(typeof(THandler), ServiceDescriptor.Singleton(typeof(THandler), instance));
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
        var handlerType = typeof(DelegateQueryHandler<TQuery, TResponse>);
        services.Replace(ServiceDescriptor.Transient(p => new DelegateQueryHandler<TQuery, TResponse>(handlerFn, p)));
        return services.AddConquerorQueryHandler(handlerType, configurePipeline);
    }

    internal static void TryAddConquerorQueryHandler(this IServiceCollection services,
                                                     Type handlerType,
                                                     ServiceDescriptor serviceDescriptor)
    {
        services.TryAdd(serviceDescriptor);
        services.AddConquerorQueryHandler(handlerType, (Delegate?)null);
    }

    private static IServiceCollection AddConquerorQueryHandler(this IServiceCollection services,
                                                               Type handlerType,
                                                               ServiceDescriptor serviceDescriptor)
    {
        services.Replace(serviceDescriptor);
        return services.AddConquerorQueryHandler(handlerType, (Delegate?)null);
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
        var pipelineConfigurationAction = configurePipeline ?? CreatePipelineConfigurationFunction(typeof(THandler));

        var existingRegistrations = services.Select(d => d.ImplementationInstance)
                                            .OfType<QueryHandlerRegistrationInternal>()
                                            .ToDictionary(r => r.QueryType);

        if (existingRegistrations.TryGetValue(typeof(TQuery), out var existingRegistration))
        {
            if (typeof(THandler) != existingRegistration.HandlerType || existingRegistration.HandlerType == typeof(DelegateQueryHandler<TQuery, TResponse>))
            {
                throw new InvalidOperationException($"attempted to register handler type '{typeof(THandler)}' for query type '{typeof(TQuery)}', but handler type '{existingRegistration.HandlerType}' is already registered.");
            }
        }
        else
        {
            var registration = new QueryHandlerRegistrationInternal(typeof(TQuery), typeof(TResponse), typeof(THandler), pipelineConfigurationAction);
            services.AddSingleton(registration);
        }

        services.AddConquerorQueryClient<THandler>(new InProcessQueryTransport(typeof(THandler), pipelineConfigurationAction));

        return services;

        static Action<IQueryPipeline<TQuery, TResponse>>? CreatePipelineConfigurationFunction(Type handlerType)
        {
            var pipelineConfigurationMethod = handlerType.GetInterfaceMap(typeof(IQueryHandler<TQuery, TResponse>))
                                                         .TargetMethods
                                                         .Where(m => m.DeclaringType == handlerType)
                                                         .SingleOrDefault(m => m.Name == nameof(IQueryHandler<TQuery, TResponse>.ConfigurePipeline));

            if (pipelineConfigurationMethod is null)
            {
                return null;
            }

            var builderParam = Expression.Parameter(typeof(IQueryPipeline<TQuery, TResponse>));
            var body = Expression.Call(null, pipelineConfigurationMethod, builderParam);
            var lambda = Expression.Lambda(body, builderParam).Compile();
            return (Action<IQueryPipeline<TQuery, TResponse>>)lambda;
        }
    }
}
