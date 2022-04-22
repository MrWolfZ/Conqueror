using System;
using System.Linq;
using Conqueror.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryServiceCollectionConfigurator : IServiceCollectionConfigurator
    {
        public int ConfigurationPhase => 1;

        public void Configure(IServiceCollection services)
        {
            ConfigureHandlers(services);
            ConfigureMiddlewares(services);
        }

        private static void ConfigureHandlers(IServiceCollection services)
        {
            var handlerTypes = services.Where(d => d.ServiceType == d.ImplementationType || d.ServiceType == d.ImplementationInstance?.GetType())
                                       .SelectMany(d => new[] { d.ImplementationType, d.ImplementationInstance?.GetType() })
                                       .OfType<Type>()
                                       .Where(t => t.IsAssignableTo(typeof(IQueryHandler)))
                                       .Distinct()
                                       .ToList();

            foreach (var handlerType in handlerTypes)
            {
                handlerType.ValidateNoInvalidQueryHandlerInterface();
                RegisterHandlerMetadata(handlerType);
                RegisterPlainInterfaces(handlerType);
                RegisterCustomerInterfaces(handlerType);
            }

            ValidateNoDuplicateQueryTypes();

            void ValidateNoDuplicateQueryTypes()
            {
                var duplicateMetadata = services.Select(d => d.ImplementationInstance).OfType<QueryHandlerMetadata>().GroupBy(t => t.QueryType).FirstOrDefault(g => g.Count() > 1);

                if (duplicateMetadata is not null)
                {
                    var queryType = duplicateMetadata.Key;
                    var duplicateHandlerTypes = duplicateMetadata.Select(h => h.HandlerType);
                    throw new InvalidOperationException($"only a single handler for query type {queryType} is allowed, but found multiple: {string.Join(", ", duplicateHandlerTypes)}");
                }
            }

            void RegisterHandlerMetadata(Type handlerType)
            {
                foreach (var (queryType, responseType) in handlerType.GetQueryAndResponseTypes())
                {
                    _ = services.AddSingleton(new QueryHandlerMetadata(queryType, responseType, handlerType));
                }
            }

            void RegisterPlainInterfaces(Type handlerType)
            {
                foreach (var (queryType, responseType) in handlerType.GetQueryAndResponseTypes())
                {
                    _ = services.AddTransient(typeof(IQueryHandler<,>).MakeGenericType(queryType, responseType), typeof(QueryHandlerProxy<,>).MakeGenericType(queryType, responseType));
                }
            }

            void RegisterCustomerInterfaces(Type handlerType)
            {
                foreach (var customInterfaceType in handlerType.GetCustomQueryHandlerInterfaceTypes())
                {
                    foreach (var plainInterfaceType in customInterfaceType.GetQueryHandlerInterfaceTypes())
                    {
                        var dynamicType = DynamicType.Create(customInterfaceType, plainInterfaceType);
                        _ = services.AddTransient(customInterfaceType, dynamicType);
                    }
                }
            }
        }

        private static void ConfigureMiddlewares(IServiceCollection services)
        {
            var middlewareTypes = services.Where(d => d.ServiceType == d.ImplementationType || d.ServiceType == d.ImplementationInstance?.GetType())
                                          .SelectMany(d => new[] { d.ImplementationType, d.ImplementationInstance?.GetType() })
                                          .OfType<Type>()
                                          .Where(t => t.IsAssignableTo(typeof(IQueryMiddleware)))
                                          .Distinct()
                                          .ToList();

            foreach (var middlewareType in middlewareTypes)
            {
                var middlewareInterfaces = middlewareType.GetInterfaces().Where(IsQueryMiddlewareInterface).ToList();

                switch (middlewareInterfaces.Count)
                {
                    case < 1:
                        continue;

                    case > 1:
                        throw new ArgumentException($"type {middlewareType.Name} implements {typeof(IQueryMiddleware<>).Name} more than once");
                }
            }

            foreach (var middlewareType in middlewareTypes)
            {
                RegisterMetadata(middlewareType);
                RegisterInvoker(middlewareType);
            }

            void RegisterMetadata(Type middlewareType)
            {
                var attributeType = middlewareType.GetInterfaces().First(IsQueryMiddlewareInterface).GetGenericArguments().First();

                _ = services.AddSingleton(new QueryMiddlewareMetadata(middlewareType, attributeType));
            }

            void RegisterInvoker(Type middlewareType)
            {
                var attributeType = middlewareType.GetInterfaces().First(IsQueryMiddlewareInterface).GetGenericArguments().First();

                _ = services.AddTransient(typeof(QueryMiddlewareInvoker<>).MakeGenericType(attributeType));
            }

            static bool IsQueryMiddlewareInterface(Type i) => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryMiddleware<>);
        }
    }
}
