using System;
using System.Linq;
using System.Reflection;
using Conqueror;
using Conqueror.CQS.QueryHandling;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable InconsistentNaming

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorCqsServiceCollectionExtensions
    {
        public static IServiceCollection AddConquerorCQS(this IServiceCollection services)
        {
            services.TryAddSingleton<QueryHandlerRegistry>();
            services.TryAddSingleton<QueryMiddlewaresInvoker>();
            services.TryAddSingleton(new QueryServiceCollectionConfigurator());
            services.TryAddSingleton<QueryContextAccessor>();
            services.TryAddSingleton<IQueryContextAccessor>(p => p.GetRequiredService<QueryContextAccessor>());

            services.AddConquerorCqsCommandServices();
            
            services.TryAddSingleton<ConquerorContextAccessor>();
            services.TryAddSingleton<IConquerorContextAccessor>(p => p.GetRequiredService<ConquerorContextAccessor>());

            return services;
        }

        public static IServiceCollection AddConquerorCQSTypesFromAssembly(this IServiceCollection services, Assembly assembly)
        {
            var validTypes = assembly.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract).ToList();

            foreach (var queryHandlerType in validTypes.Where(t => t.IsAssignableTo(typeof(IQueryHandler))))
            {
                services.TryAddTransient(queryHandlerType);
            }

            foreach (var queryMiddlewareType in validTypes.Where(t => t.GetInterfaces().Any(IsQueryMiddlewareInterface)))
            {
                services.TryAddTransient(queryMiddlewareType);
            }

            foreach (var commandHandlerType in validTypes.Where(t => t.IsAssignableTo(typeof(ICommandHandler))))
            {
                services.TryAddTransient(commandHandlerType);
            }

            foreach (var commandMiddlewareType in validTypes.Where(t => t.GetInterfaces().Any(IsCommandMiddlewareInterface)))
            {
                services.TryAddTransient(commandMiddlewareType);
            }

            return services;

            static bool IsQueryMiddlewareInterface(Type i) => i == typeof(IQueryMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryMiddleware<>));
            static bool IsCommandMiddlewareInterface(Type i) => i == typeof(ICommandMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandMiddleware<>));
        }

        public static IServiceCollection AddConquerorCQSTypesFromExecutingAssembly(this IServiceCollection services)
        {
            return services.AddConquerorCQSTypesFromAssembly(Assembly.GetCallingAssembly());
        }
    }
}
