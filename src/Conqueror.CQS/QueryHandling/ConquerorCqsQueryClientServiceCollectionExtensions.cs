using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Common;
using Conqueror.CQS.QueryHandling;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorCqsQueryClientServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorQueryClient<THandler>(this IServiceCollection services,
                                                                       Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory)
        where THandler : class, IQueryHandler
    {
        return services.AddConquerorQueryClient(typeof(THandler), transportClientFactory, null);
    }

    public static IServiceCollection AddConquerorQueryClient<THandler>(this IServiceCollection services,
                                                                       Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory,
                                                                       Action<IQueryPipelineBuilder> configurePipeline)
        where THandler : class, IQueryHandler
    {
        return services.AddConquerorQueryClient(typeof(THandler), transportClientFactory, configurePipeline);
    }

    public static IServiceCollection AddConquerorQueryClient<THandler>(this IServiceCollection services,
                                                                       Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>> transportClientFactory)
        where THandler : class, IQueryHandler
    {
        return services.AddConquerorQueryClient(typeof(THandler), transportClientFactory, null);
    }

    public static IServiceCollection AddConquerorQueryClient<THandler>(this IServiceCollection services,
                                                                       Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>> transportClientFactory,
                                                                       Action<IQueryPipelineBuilder> configurePipeline)
        where THandler : class, IQueryHandler
    {
        return services.AddConquerorQueryClient(typeof(THandler), transportClientFactory, configurePipeline);
    }

    internal static IServiceCollection AddConquerorQueryClient(this IServiceCollection services,
                                                               Type handlerType,
                                                               IQueryTransportClient transportClient,
                                                               Action<IQueryPipelineBuilder>? configurePipeline)
    {
        return services.AddConquerorQueryClient(handlerType, new QueryTransportClientFactory(transportClient), configurePipeline);
    }

    private static IServiceCollection AddConquerorQueryClient(this IServiceCollection services,
                                                              Type handlerType,
                                                              Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory,
                                                              Action<IQueryPipelineBuilder>? configurePipeline)
    {
        return services.AddConquerorQueryClient(handlerType, new QueryTransportClientFactory(transportClientFactory), configurePipeline);
    }

    private static IServiceCollection AddConquerorQueryClient(this IServiceCollection services,
                                                              Type handlerType,
                                                              Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>> transportClientFactory,
                                                              Action<IQueryPipelineBuilder>? configurePipeline)
    {
        return services.AddConquerorQueryClient(handlerType, new QueryTransportClientFactory(transportClientFactory), configurePipeline);
    }

    private static IServiceCollection AddConquerorQueryClient(this IServiceCollection services,
                                                              Type handlerType,
                                                              QueryTransportClientFactory transportClientFactory,
                                                              Action<IQueryPipelineBuilder>? configurePipeline)
    {
        handlerType.ValidateNoInvalidQueryHandlerInterface();

        services.AddConquerorCqsQueryServices();

        var addClientMethod = typeof(ConquerorCqsQueryClientServiceCollectionExtensions).GetMethod(nameof(AddClient), BindingFlags.NonPublic | BindingFlags.Static);

        if (addClientMethod == null)
        {
            throw new InvalidOperationException($"could not find method '{nameof(AddClient)}'");
        }

        var existingQueryRegistrations = services.Select(d => d.ServiceType)
                                                 .Where(t => t.IsQueryHandlerInterfaceType())
                                                 .SelectMany(t => t.GetQueryAndResponseTypes())
                                                 .ToDictionary(t => t.QueryType, t => t.ResponseType);

        foreach (var (queryType, responseType) in handlerType.GetQueryAndResponseTypes())
        {
            if (existingQueryRegistrations.TryGetValue(queryType, out var existingResponseType) && responseType != existingResponseType)
            {
                throw new InvalidOperationException($"client for query type '{queryType.Name}' is already registered with response type '{existingResponseType.Name}', but tried to add client with different response type '{responseType.Name}'");
            }

            var genericAddClientMethod = addClientMethod.MakeGenericMethod(handlerType, queryType, responseType);

            try
            {
                _ = genericAddClientMethod.Invoke(null, new object?[] { services, transportClientFactory, configurePipeline });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        return services;
    }

    private static void AddClient<THandler, TQuery, TResponse>(this IServiceCollection services,
                                                               QueryTransportClientFactory transportClientFactory,
                                                               Action<IQueryPipelineBuilder>? configurePipeline = null)
        where THandler : class, IQueryHandler
        where TQuery : class
    {
        RegisterPlainInterface();
        RegisterCustomInterface();

        void RegisterPlainInterface()
        {
            _ = services.Replace(ServiceDescriptor.Transient<IQueryHandler<TQuery, TResponse>>(CreateProxy));
        }

        QueryHandlerProxy<TQuery, TResponse> CreateProxy(IServiceProvider serviceProvider)
        {
            var queryMiddlewareRegistry = serviceProvider.GetRequiredService<QueryMiddlewareRegistry>();
            return new(serviceProvider, transportClientFactory, configurePipeline, queryMiddlewareRegistry);
        }

        void RegisterCustomInterface()
        {
            if (GetCustomQueryHandlerInterfaceType() is { } customInterfaceType)
            {
                var dynamicType = DynamicType.Create(customInterfaceType, typeof(IQueryHandler<TQuery, TResponse>));
                services.TryAddTransient(customInterfaceType, dynamicType);
            }
        }

        static Type? GetCustomQueryHandlerInterfaceType()
        {
            var interfaces = typeof(THandler).GetInterfaces()
                                             .Concat(new[] { typeof(THandler) })
                                             .Where(i => i.IsCustomQueryHandlerInterfaceType<TQuery, TResponse>())
                                             .ToList();

            if (interfaces.Count < 1)
            {
                return null;
            }

            if (interfaces.Count > 1)
            {
                throw new InvalidOperationException($"query handler type '{typeof(THandler).Name}' implements more than one custom interface for query '{typeof(TQuery).Name}'");
            }

            var customHandlerInterface = interfaces.Single();

            if (customHandlerInterface.AllMethods().Count() > 1)
            {
                throw new ArgumentException($"query handler type '{typeof(THandler).Name}' implements custom interface '{customHandlerInterface.Name}' that has extra methods; custom query handler interface types are not allowed to have any additional methods beside the '{nameof(IQueryHandler<object, object>.ExecuteQuery)}' inherited from '{typeof(IQueryHandler<,>).Name}'");
            }

            return customHandlerInterface;
        }
    }
}
