using System.Linq;
using System.Reflection;
using Conqueror;
using Conqueror.Common;
using Conqueror.Streaming;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorStreamingServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorStreaming(this IServiceCollection services)
    {
        services.AddFinalizationCheck();

        services.TryAddSingleton<StreamingHandlerRegistry>();
        //// TODO
        //// services.TryAddSingleton<StreamingMiddlewaresInvoker>();
        services.TryAddSingleton(new StreamingRegistrationFinalizer(services));
        //// TODO
        //// services.TryAddSingleton<StreamingContextAccessor>();
        //// services.TryAddSingleton<IStreamingContextAccessor>(p => p.GetRequiredService<StreamingContextAccessor>());

        services.TryAddSingleton<DefaultConquerorContextAccessor>();
        services.TryAddSingleton<IConquerorContextAccessor>(p => p.GetRequiredService<DefaultConquerorContextAccessor>());

        return services;
    }

    public static IServiceCollection AddConquerorStreamingTypesFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var validTypes = assembly.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract).ToList();

        foreach (var handlerType in validTypes.Where(t => t.IsAssignableTo(typeof(IStreamingHandler))))
        {
            services.TryAddTransient(handlerType);
        }

        // TODO
        // foreach (var middlewareType in validTypes.Where(t => t.GetInterfaces().Any(IsStreamingMiddlewareInterface)))
        // {
        //     services.TryAddTransient(middlewareType);
        // }

        return services;

        // static bool IsStreamingMiddlewareInterface(Type i) => i == typeof(IStreamingMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamingMiddleware<>));
    }

    public static IServiceCollection AddConquerorStreamingTypesFromExecutingAssembly(this IServiceCollection services)
    {
        return services.AddConquerorStreamingTypesFromAssembly(Assembly.GetCallingAssembly());
    }
}
