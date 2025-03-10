using System.Linq;
using System.Reflection;
using Conqueror;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorServiceCollectionExtensions
{
    public static IServiceCollection AddConqueror(this IServiceCollection services)
    {
        // services.AddConquerorMessageServices();

        return services;
    }

    public static IServiceCollection AddConquerorCQSTypesFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var validTypes = assembly.GetTypes()
                                 .Where(t => t is { IsInterface: false, IsAbstract: false, ContainsGenericParameters: false, IsNestedPrivate: false })
                                 .ToList();

        foreach (var messageHandlerType in validTypes.Where(t => t.IsAssignableTo(typeof(IMessageHandler))))
        {
            // services.TryAddConquerorMessageHandler(messageHandlerType, ServiceDescriptor.Transient(messageHandlerType, messageHandlerType));
        }

        return services;
    }

    public static IServiceCollection AddConquerorCQSTypesFromExecutingAssembly(this IServiceCollection services)
    {
        return services.AddConquerorCQSTypesFromAssembly(Assembly.GetCallingAssembly());
    }
}
