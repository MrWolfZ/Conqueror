using System;
using System.Linq;
using System.Reflection;
using Conqueror;
using Conqueror.Streaming;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorStreamingServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorStreaming(this IServiceCollection services)
    {
        services.TryAddTransient<IStreamingRequestClientFactory, TransientStreamingRequestHandlerClientFactory>();
        services.TryAddSingleton<StreamingRequestClientFactory>();
        services.TryAddSingleton<StreamingRequestHandlerRegistry>();
        services.TryAddSingleton<IStreamingRequestHandlerRegistry>(p => p.GetRequiredService<StreamingRequestHandlerRegistry>());
        services.TryAddSingleton<StreamingRequestMiddlewareRegistry>();

        services.AddConquerorContext();

        return services;
    }

    public static IServiceCollection AddConquerorStreamingTypesFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var validTypes = assembly.GetTypes()
                                 .Where(t => t is { IsInterface: false, IsAbstract: false, ContainsGenericParameters: false, IsNestedPrivate: false })
                                 .ToList();

        foreach (var requestHandlerType in validTypes.Where(t => t.IsAssignableTo(typeof(IStreamingRequestHandler))))
        {
            services.AddConquerorStreamingRequestHandler(requestHandlerType, ServiceDescriptor.Transient(requestHandlerType, requestHandlerType));
        }

        foreach (var requestMiddlewareType in validTypes.Where(t => Array.Exists(t.GetInterfaces(), IsStreamingRequestMiddlewareInterface)))
        {
            services.AddConquerorStreamingRequestMiddleware(requestMiddlewareType, ServiceDescriptor.Transient(requestMiddlewareType, requestMiddlewareType));
        }

        return services;

        static bool IsStreamingRequestMiddlewareInterface(Type i) => i == typeof(IStreamingRequestMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamingRequestMiddleware<>));
    }

    public static IServiceCollection AddConquerorStreamingTypesFromExecutingAssembly(this IServiceCollection services)
    {
        return services.AddConquerorStreamingTypesFromAssembly(Assembly.GetCallingAssembly());
    }
}
