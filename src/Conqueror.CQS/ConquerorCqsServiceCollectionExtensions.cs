using System.Linq;
using System.Reflection;
using Conqueror;

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
            services.TryAddConquerorCommandHandler(commandHandlerType, ServiceDescriptor.Transient(commandHandlerType, commandHandlerType));
        }

        foreach (var queryHandlerType in validTypes.Where(t => t.IsAssignableTo(typeof(IQueryHandler))))
        {
            services.TryAddConquerorQueryHandler(queryHandlerType, ServiceDescriptor.Transient(queryHandlerType, queryHandlerType));
        }

        return services;
    }

    public static IServiceCollection AddConquerorCQSTypesFromExecutingAssembly(this IServiceCollection services)
    {
        return services.AddConquerorCQSTypesFromAssembly(Assembly.GetCallingAssembly());
    }
}
