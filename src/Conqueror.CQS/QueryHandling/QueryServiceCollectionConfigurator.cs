using System;
using System.Linq;
using Conqueror.Util;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryServiceCollectionConfigurator : IServiceCollectionConfigurator
    {
        public void Configure(IServiceCollection services)
        {
            ConfigureQueryHandlers(services);
            ConfigureQueryMiddlewares(services);
        }

        private static void ConfigureQueryHandlers(IServiceCollection services)
        {
            var handlerTypes = services.Where(d => d.ServiceType == d.ImplementationType)
                                       .Select(d => d.ImplementationType)
                                       .OfType<Type>()
                                       .Where(t => t.IsAssignableTo(typeof(IQueryHandler)))
                                       .ToList();

            foreach (var handlerType in handlerTypes)
            {
                var (queryType, responseType) = handlerType.GetQueryAndResponseType();

                var metadata = new QueryHandlerMetadata(queryType, responseType, handlerType);

                _ = services.AddSingleton(metadata);

                var customInterfaceType = handlerType.GetCustomQueryHandlerInterfaceType();
                var plainInterfaceType = handlerType.GetQueryHandlerInterfaceType();

                if (customInterfaceType is not null)
                {
                    var dynamicType = DynamicType.Create(customInterfaceType, plainInterfaceType);
                    _ = services.AddTransient(customInterfaceType, dynamicType);
                }
            }
        }

        private static void ConfigureQueryMiddlewares(IServiceCollection services)
        {
            foreach (var middlewareType in services.Where(d => d.ServiceType == d.ImplementationType).Select(d => d.ImplementationType).OfType<Type>().ToList())
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

            static bool IsQueryMiddlewareInterface(Type i) => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryMiddleware<>);
        }
    }
}
