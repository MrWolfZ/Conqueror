using System;
using System.Linq;
using System.Reflection;
using Conqueror;

// ReSharper disable InconsistentNaming

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorCqsServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorCQS(this IServiceCollection services)
    {
        services.AddConquerorCqsCommandServices();
        services.AddConquerorCqsQueryServices();

        return services;
    }

    public static IServiceCollection AddConquerorCQSTypesFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var validTypes = assembly.GetTypes()
                                 .Where(t => t is { IsInterface: false, IsAbstract: false, ContainsGenericParameters: false, IsNestedPrivate: false })
                                 .ToList();

        foreach (var commandHandlerType in validTypes.Where(t => t.IsAssignableTo(typeof(ICommandHandler))))
        {
            services.AddConquerorCommandHandler(commandHandlerType, ServiceDescriptor.Transient(commandHandlerType, commandHandlerType));
        }

        foreach (var commandMiddlewareType in validTypes.Where(t => t.GetInterfaces().Any(IsCommandMiddlewareInterface)))
        {
            services.AddConquerorCommandMiddleware(commandMiddlewareType, ServiceDescriptor.Transient(commandMiddlewareType, commandMiddlewareType));
        }

        foreach (var queryHandlerType in validTypes.Where(t => t.IsAssignableTo(typeof(IQueryHandler))))
        {
            services.AddConquerorQueryHandler(queryHandlerType, ServiceDescriptor.Transient(queryHandlerType, queryHandlerType));
        }

        foreach (var queryMiddlewareType in validTypes.Where(t => t.GetInterfaces().Any(IsQueryMiddlewareInterface)))
        {
            services.AddConquerorQueryMiddleware(queryMiddlewareType, ServiceDescriptor.Transient(queryMiddlewareType, queryMiddlewareType));
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
