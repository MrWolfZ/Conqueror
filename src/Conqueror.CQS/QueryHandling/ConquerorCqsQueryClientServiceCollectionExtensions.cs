using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Conqueror;
using Conqueror.Common;
using Conqueror.CQS.Common;
using Conqueror.CQS.QueryHandling;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorCqsQueryClientServiceCollectionExtensions
    {
        public static IServiceCollection AddConquerorQueryClient<THandler>(this IServiceCollection services,
                                                                           Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory,
                                                                           Action<IQueryPipelineBuilder>? configurePipeline = null)
            where THandler : class, IQueryHandler
        {
            return services.AddConquerorQueryClient(typeof(THandler), transportClientFactory, configurePipeline);
        }

        public static IServiceCollection AddConquerorQueryClient<THandler>(this IServiceCollection services,
                                                                           Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>> transportClientFactory,
                                                                           Action<IQueryPipelineBuilder>? configurePipeline = null)
            where THandler : class, IQueryHandler
        {
            return services.AddConquerorQueryClient(typeof(THandler), transportClientFactory, configurePipeline, false);
        }

        internal static IServiceCollection TryAddConquerorQueryClient(this IServiceCollection services,
                                                                      Type handlerType,
                                                                      Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory,
                                                                      Action<IQueryPipelineBuilder>? configurePipeline)
        {
            return services.AddConquerorQueryClient(handlerType, b => Task.FromResult(transportClientFactory(b)), configurePipeline, true);
        }

        internal static IServiceCollection AddConquerorQueryClient(this IServiceCollection services,
                                                                   Type handlerType,
                                                                   Func<IQueryTransportClientBuilder, IQueryTransportClient> transportClientFactory,
                                                                   Action<IQueryPipelineBuilder>? configurePipeline)
        {
            return services.AddConquerorQueryClient(handlerType, b => Task.FromResult(transportClientFactory(b)), configurePipeline, false);
        }

        internal static IServiceCollection AddConquerorQueryClient(this IServiceCollection services,
                                                                   Type handlerType,
                                                                   Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>> transportClientFactory,
                                                                   Action<IQueryPipelineBuilder>? configurePipeline,
                                                                   bool shouldIgnoreOnDuplicate)
        {
            handlerType.ValidateNoInvalidQueryHandlerInterface();

            services.AddConquerorCqsQueryServices();

            var addClientMethod = typeof(ConquerorCqsQueryClientServiceCollectionExtensions).GetMethod(nameof(AddClient), BindingFlags.NonPublic | BindingFlags.Static);

            if (addClientMethod == null)
            {
                throw new InvalidOperationException($"could not find method '{nameof(AddClient)}'");
            }

            foreach (var (queryType, responseType) in handlerType.GetQueryAndResponseTypes())
            {
                if (services.Any(d => d.ServiceType == typeof(IQueryHandler<,>).MakeGenericType(queryType, responseType)))
                {
                    if (shouldIgnoreOnDuplicate)
                    {
                        continue;
                    }

                    throw new InvalidOperationException($"query client for handler type '{handlerType.Name}' is already registered");
                }

                var genericAddClientMethod = addClientMethod.MakeGenericMethod(handlerType, queryType, responseType);

                try
                {
                    _ = genericAddClientMethod.Invoke(null, new object?[] { services, transportClientFactory, configurePipeline });
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
            }

            return services;
        }

        private static void AddClient<THandler, TQuery, TResponse>(this IServiceCollection services,
                                                                   Func<IQueryTransportClientBuilder, Task<IQueryTransportClient>> transportClientFactory,
                                                                   Action<IQueryPipelineBuilder>? configurePipeline = null)
            where THandler : class, IQueryHandler
            where TQuery : class
        {
            RegisterPlainInterface();
            RegisterCustomInterface();

            void RegisterPlainInterface()
            {
                services.TryAddTransient<IQueryHandler<TQuery, TResponse>>(
                    p => new QueryHandlerProxy<TQuery, TResponse>(p, transportClientFactory, configurePipeline));
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
                    throw new ArgumentException(
                        $"query handler type '{typeof(THandler).Name}' implements custom interface '{customHandlerInterface.Name}' that has extra methods; custom query handler interface types are not allowed to have any additional methods beside the '{nameof(IQueryHandler<object, object>.ExecuteQuery)}' inherited from '{typeof(IQueryHandler<,>).Name}'");
                }

                return customHandlerInterface;
            }
        }
    }
}
