using System;
using System.Linq;
using System.Reflection;
using Conqueror;
using Conqueror.Streaming;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorStreamConsumerMiddlewareServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorStreamConsumerMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                       ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TMiddleware : class, IStreamConsumerMiddlewareMarker
    {
        return services.AddConquerorStreamConsumerMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), typeof(TMiddleware), lifetime));
    }

    public static IServiceCollection AddConquerorStreamConsumerMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                       Func<IServiceProvider, TMiddleware> factory,
                                                                                       ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TMiddleware : class, IStreamConsumerMiddlewareMarker
    {
        return services.AddConquerorStreamConsumerMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), factory, lifetime));
    }

    public static IServiceCollection AddConquerorStreamConsumerMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                       TMiddleware instance)
        where TMiddleware : class, IStreamConsumerMiddlewareMarker
    {
        return services.AddConquerorStreamConsumerMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), instance));
    }

    public static IServiceCollection AddConquerorStreamConsumerMiddleware(this IServiceCollection services,
                                                                          Type middlewareType,
                                                                          ServiceDescriptor serviceDescriptor)
    {
        if (services.Any(d => d.ServiceType == middlewareType))
        {
            return services;
        }

        services.Add(serviceDescriptor);
        return services.AddConquerorStreamConsumerMiddleware(middlewareType);
    }

    private static IServiceCollection AddConquerorStreamConsumerMiddleware(this IServiceCollection services,
                                                                           Type middlewareType)
    {
        var middlewareInterfaces = middlewareType.GetInterfaces().Where(IsStreamConsumerMiddlewareInterface).ToList();

        switch (middlewareInterfaces.Count)
        {
            case < 1:
                throw new ArgumentException($"type '{middlewareType.Name}' implements no stream consumer middleware interface");

            case > 1:
                throw new ArgumentException($"type {middlewareType.Name} implements {nameof(IStreamConsumerMiddleware)} more than once");
        }

        services.AddConquerorStreaming();

        var configurationMethod = typeof(ConquerorStreamConsumerMiddlewareServiceCollectionExtensions).GetMethod(nameof(ConfigureMiddleware), BindingFlags.NonPublic | BindingFlags.Static);

        if (configurationMethod == null)
        {
            throw new InvalidOperationException($"could not find middleware configuration method '{nameof(ConfigureMiddleware)}'");
        }

        var genericConfigurationMethod = configurationMethod.MakeGenericMethod(middlewareType, GetMiddlewareConfigurationType(middlewareType) ?? typeof(NullStreamConsumerMiddlewareConfiguration));

        try
        {
            _ = genericConfigurationMethod.Invoke(null, [services]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }

        return services;
    }

    private static void ConfigureMiddleware<TMiddleware, TConfiguration>(IServiceCollection services)
    {
        _ = services.AddSingleton<IStreamConsumerMiddlewareInvoker, StreamConsumerMiddlewareInvoker<TMiddleware, TConfiguration>>();
    }

    private static Type? GetMiddlewareConfigurationType(Type t) => t.GetInterfaces().First(IsStreamConsumerMiddlewareInterface).GetGenericArguments().FirstOrDefault();

    private static bool IsStreamConsumerMiddlewareInterface(Type i) => i == typeof(IStreamConsumerMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamConsumerMiddleware<>));
}
