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
        return services.AddConquerorQueryClient(typeof(THandler), transportClientFactory);
    }

    public static IServiceCollection AddConquerorQueryClient<THandler>(this IServiceCollection services,
                                                                       Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>> transportClientFactory)
        where THandler : class, IQueryHandler
    {
        return services.AddConquerorQueryClient(typeof(THandler), transportClientFactory);
    }

    internal static IServiceCollection AddConquerorQueryClient<THandler>(this IServiceCollection services,
                                                                         IQueryTransportClient transportClient)
        where THandler : class, IQueryHandler
    {
        return services.AddConquerorQueryClient(typeof(THandler), new QueryTransportClientFactory(transportClient));
    }

    private static IServiceCollection AddConquerorQueryClient(this IServiceCollection services,
                                                              Type handlerType,
                                                              Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory)
    {
        return services.AddConquerorQueryClient(handlerType, new QueryTransportClientFactory(transportClientFactory));
    }

    private static IServiceCollection AddConquerorQueryClient(this IServiceCollection services,
                                                              Type handlerType,
                                                              Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>> transportClientFactory)
    {
        return services.AddConquerorQueryClient(handlerType, new QueryTransportClientFactory(transportClientFactory));
    }

    private static IServiceCollection AddConquerorQueryClient(this IServiceCollection services,
                                                              Type handlerType,
                                                              QueryTransportClientFactory transportClientFactory)
    {
        handlerType.ValidateNoInvalidQueryHandlerInterface();

        var addClientMethod = typeof(ConquerorCqsQueryClientServiceCollectionExtensions).GetMethod(nameof(AddClient), BindingFlags.NonPublic | BindingFlags.Static);

        if (addClientMethod == null)
        {
            throw new InvalidOperationException($"could not find method '{nameof(AddClient)}'");
        }

        foreach (var (queryType, responseType) in handlerType.GetQueryAndResponseTypes())
        {
            var genericAddClientMethod = addClientMethod.MakeGenericMethod(handlerType, queryType, responseType);

            try
            {
                _ = genericAddClientMethod.Invoke(null, [services, transportClientFactory]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
        }

        return services;
    }

    private static void AddClient<THandler, TQuery, TResponse>(this IServiceCollection services,
                                                               QueryTransportClientFactory transportClientFactory)
        where THandler : class, IQueryHandler
        where TQuery : class
    {
        typeof(THandler).ValidateNoInvalidQueryHandlerInterface();

        var existingQueryRegistrations = services.Select(d => d.ServiceType)
                                                 .Where(t => t.IsQueryHandlerInterfaceType())
                                                 .SelectMany(t => t.GetQueryAndResponseTypes())
                                                 .ToDictionary(t => t.QueryType, t => t.ResponseType);

        if (existingQueryRegistrations.TryGetValue(typeof(TQuery), out var existingResponseType) && typeof(TResponse) != existingResponseType)
        {
            throw new InvalidOperationException($"client for query type '{typeof(TQuery)}' is already registered with response type '{existingResponseType}', but tried to add client with different response type '{typeof(TResponse)}'");
        }

        services.AddConquerorCqsQueryServices();

        RegisterPlainInterface();
        RegisterCustomInterface();

        void RegisterPlainInterface()
        {
            _ = services.Replace(ServiceDescriptor.Transient<IQueryHandler<TQuery, TResponse>>(CreateProxy));
        }

        QueryHandlerProxy<TQuery, TResponse> CreateProxy(IServiceProvider serviceProvider)
        {
            return new(serviceProvider, transportClientFactory, null);
        }

        void RegisterCustomInterface()
        {
            if (GetCustomQueryHandlerInterfaceType() is { } customInterfaceType)
            {
                var proxyType = ProxyTypeGenerator.Create(customInterfaceType, typeof(IQueryHandler<TQuery, TResponse>), typeof(QueryHandlerGeneratedProxyBase<TQuery, TResponse>));
                services.TryAddTransient(customInterfaceType, proxyType);
            }
        }

        static Type? GetCustomQueryHandlerInterfaceType()
        {
            var interfaces = typeof(THandler).GetInterfaces()
                                             .Concat([typeof(THandler)])
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
