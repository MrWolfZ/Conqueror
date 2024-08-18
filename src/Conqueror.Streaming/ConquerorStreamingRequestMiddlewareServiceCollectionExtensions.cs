using System;
using System.Linq;
using System.Reflection;
using Conqueror;
using Conqueror.Streaming;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorStreamingRequestMiddlewareServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorStreamingRequestMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                         ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TMiddleware : class, IStreamingRequestMiddlewareMarker
    {
        return services.AddConquerorStreamingRequestMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), typeof(TMiddleware), lifetime));
    }

    public static IServiceCollection AddConquerorStreamingRequestMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                         Func<IServiceProvider, TMiddleware> factory,
                                                                                         ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TMiddleware : class, IStreamingRequestMiddlewareMarker
    {
        return services.AddConquerorStreamingRequestMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), factory, lifetime));
    }

    public static IServiceCollection AddConquerorStreamingRequestMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                         TMiddleware instance)
        where TMiddleware : class, IStreamingRequestMiddlewareMarker
    {
        return services.AddConquerorStreamingRequestMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), instance));
    }

    public static IServiceCollection AddConquerorStreamingRequestMiddleware(this IServiceCollection services,
                                                                            Type middlewareType,
                                                                            ServiceDescriptor serviceDescriptor)
    {
        if (services.Any(d => d.ServiceType == middlewareType))
        {
            return services;
        }

        services.Add(serviceDescriptor);
        return services.AddConquerorStreamingRequestMiddleware(middlewareType);
    }

    internal static IServiceCollection AddConquerorStreamingRequestMiddleware(this IServiceCollection services,
                                                                              Type middlewareType)
    {
        var middlewareInterfaces = middlewareType.GetInterfaces().Where(IsStreamingRequestMiddlewareInterface).ToList();

        switch (middlewareInterfaces.Count)
        {
            case < 1:
                throw new ArgumentException($"type '{middlewareType.Name}' implements no streaming request middleware interface");

            case > 1:
                throw new ArgumentException($"type {middlewareType.Name} implements {nameof(IStreamingRequestMiddleware)} more than once");
        }

        services.AddConquerorStreaming();

        var configurationMethod = typeof(ConquerorStreamingRequestMiddlewareServiceCollectionExtensions).GetMethod(nameof(ConfigureMiddleware), BindingFlags.NonPublic | BindingFlags.Static);

        if (configurationMethod == null)
        {
            throw new InvalidOperationException($"could not find middleware configuration method '{nameof(ConfigureMiddleware)}'");
        }

        var genericConfigurationMethod = configurationMethod.MakeGenericMethod(middlewareType, GetMiddlewareConfigurationType(middlewareType) ?? typeof(NullStreamingRequestMiddlewareConfiguration));

        try
        {
            _ = genericConfigurationMethod.Invoke(null, new object[] { services });
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }

        return services;
    }

    private static void ConfigureMiddleware<TMiddleware, TConfiguration>(IServiceCollection services)
    {
        _ = services.AddSingleton<IStreamingRequestMiddlewareInvoker, StreamingRequestMiddlewareInvoker<TMiddleware, TConfiguration>>();
    }

    private static Type? GetMiddlewareConfigurationType(Type t) => t.GetInterfaces().First(IsStreamingRequestMiddlewareInterface).GetGenericArguments().FirstOrDefault();

    private static bool IsStreamingRequestMiddlewareInterface(Type i) => i == typeof(IStreamingRequestMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamingRequestMiddleware<>));
}
