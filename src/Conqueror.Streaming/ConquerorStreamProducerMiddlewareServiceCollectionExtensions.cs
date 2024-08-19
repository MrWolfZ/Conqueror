using System;
using System.Linq;
using System.Reflection;
using Conqueror;
using Conqueror.Streaming;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorStreamProducerMiddlewareServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorStreamProducerMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                       ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TMiddleware : class, IStreamProducerMiddlewareMarker
    {
        return services.AddConquerorStreamProducerMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), typeof(TMiddleware), lifetime));
    }

    public static IServiceCollection AddConquerorStreamProducerMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                       Func<IServiceProvider, TMiddleware> factory,
                                                                                       ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TMiddleware : class, IStreamProducerMiddlewareMarker
    {
        return services.AddConquerorStreamProducerMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), factory, lifetime));
    }

    public static IServiceCollection AddConquerorStreamProducerMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                       TMiddleware instance)
        where TMiddleware : class, IStreamProducerMiddlewareMarker
    {
        return services.AddConquerorStreamProducerMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), instance));
    }

    public static IServiceCollection AddConquerorStreamProducerMiddleware(this IServiceCollection services,
                                                                          Type middlewareType,
                                                                          ServiceDescriptor serviceDescriptor)
    {
        if (services.Any(d => d.ServiceType == middlewareType))
        {
            return services;
        }

        services.Add(serviceDescriptor);
        return services.AddConquerorStreamProducerMiddleware(middlewareType);
    }

    internal static IServiceCollection AddConquerorStreamProducerMiddleware(this IServiceCollection services,
                                                                            Type middlewareType)
    {
        var middlewareInterfaces = middlewareType.GetInterfaces().Where(IsStreamProducerMiddlewareInterface).ToList();

        switch (middlewareInterfaces.Count)
        {
            case < 1:
                throw new ArgumentException($"type '{middlewareType.Name}' implements no stream producer middleware interface");

            case > 1:
                throw new ArgumentException($"type {middlewareType.Name} implements {nameof(IStreamProducerMiddleware)} more than once");
        }

        services.AddConquerorStreaming();

        var configurationMethod = typeof(ConquerorStreamProducerMiddlewareServiceCollectionExtensions).GetMethod(nameof(ConfigureMiddleware), BindingFlags.NonPublic | BindingFlags.Static);

        if (configurationMethod == null)
        {
            throw new InvalidOperationException($"could not find middleware configuration method '{nameof(ConfigureMiddleware)}'");
        }

        var genericConfigurationMethod = configurationMethod.MakeGenericMethod(middlewareType, GetMiddlewareConfigurationType(middlewareType) ?? typeof(NullStreamProducerMiddlewareConfiguration));

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
        _ = services.AddSingleton<IStreamProducerMiddlewareInvoker, StreamProducerMiddlewareInvoker<TMiddleware, TConfiguration>>();
    }

    private static Type? GetMiddlewareConfigurationType(Type t) => t.GetInterfaces().First(IsStreamProducerMiddlewareInterface).GetGenericArguments().FirstOrDefault();

    private static bool IsStreamProducerMiddlewareInterface(Type i) => i == typeof(IStreamProducerMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamProducerMiddleware<>));
}
