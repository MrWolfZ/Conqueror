using System;
using System.Linq;
using System.Reflection;
using Conqueror.CQS;
using Conqueror.CQS.CommandHandling;
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
            services.TryAddTransient(typeof(IQueryHandler<,>), typeof(QueryHandlerProxy<,>));
            services.TryAddSingleton(typeof(QueryMiddlewareInvoker<>));
            services.TryAddSingleton<QueryHandlerRegistry>();
            services.TryAddSingleton<QueryMiddlewaresInvoker>();
            services.TryAddSingleton(new QueryServiceCollectionConfigurator());

            services.TryAddTransient(typeof(ICommandHandler<,>), typeof(CommandHandlerProxy<,>));
            services.TryAddTransient(typeof(ICommandHandler<>), typeof(CommandHandlerProxy<>));
            services.TryAddSingleton(typeof(CommandMiddlewareInvoker<>));
            services.TryAddSingleton<CommandHandlerRegistry>();
            services.TryAddSingleton<CommandMiddlewaresInvoker>();
            services.TryAddSingleton(new CommandServiceCollectionConfigurator());

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

            static bool IsQueryMiddlewareInterface(Type i) => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryMiddleware<>);
            static bool IsCommandMiddlewareInterface(Type i) => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandMiddleware<>);
        }

        public static IServiceCollection AddConquerorCQSTypesFromExecutingAssembly(this IServiceCollection services)
        {
            return services.AddConquerorCQSTypesFromAssembly(Assembly.GetCallingAssembly());
        }
    }
}
