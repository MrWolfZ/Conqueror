using System;
using System.Linq;
using System.Reflection;
using Conqueror;
using Conqueror.Eventing;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorEventingObserverMiddlewareServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorEventObserverMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                      ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TMiddleware : class, IEventObserverMiddlewareMarker
    {
        return services.AddConquerorEventObserverMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), typeof(TMiddleware), lifetime));
    }

    public static IServiceCollection AddConquerorEventObserverMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                      Func<IServiceProvider, TMiddleware> factory,
                                                                                      ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TMiddleware : class, IEventObserverMiddlewareMarker
    {
        return services.AddConquerorEventObserverMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), factory, lifetime));
    }

    public static IServiceCollection AddConquerorEventObserverMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                      TMiddleware instance)
        where TMiddleware : class, IEventObserverMiddlewareMarker
    {
        return services.AddConquerorEventObserverMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), instance));
    }

    internal static IServiceCollection AddConquerorEventObserverMiddleware(this IServiceCollection services,
                                                                           Type middlewareType,
                                                                           ServiceDescriptor serviceDescriptor)
    {
        services.AddConquerorEventing();

        services.Replace(serviceDescriptor);

        return services.AddConquerorEventObserverMiddleware(middlewareType);
    }

    internal static bool IsEventObserverMiddlewareRegistered(this IServiceCollection services, Type middlewareType)
    {
        var registrationCheckMethod = typeof(ConquerorEventingObserverMiddlewareServiceCollectionExtensions).GetMethod(nameof(IsMiddlewareRegistered), BindingFlags.NonPublic | BindingFlags.Static);

        if (registrationCheckMethod == null)
        {
            throw new InvalidOperationException($"could not find middleware registration check method '{nameof(IsMiddlewareRegistered)}'");
        }

        var genericRegistrationCheckMethod = registrationCheckMethod.MakeGenericMethod(middlewareType, GetMiddlewareConfigurationType(middlewareType) ?? typeof(NullObserverMiddlewareConfiguration));

        try
        {
            return (bool)genericRegistrationCheckMethod.Invoke(null, new object[] { services })!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    private static IServiceCollection AddConquerorEventObserverMiddleware(this IServiceCollection services,
                                                                          Type middlewareType)
    {
        var middlewareInterfaces = middlewareType.GetInterfaces().Where(IsEventObserverMiddlewareInterface).ToList();

        switch (middlewareInterfaces.Count)
        {
            case < 1:
                throw new ArgumentException($"type '{middlewareType.Name}' implements no observer middleware interface");

            case > 1:
                throw new ArgumentException($"type {middlewareType.Name} implements {nameof(IEventObserverMiddleware)} more than once");
        }

        var configurationMethod = typeof(ConquerorEventingObserverMiddlewareServiceCollectionExtensions).GetMethod(nameof(ConfigureMiddleware), BindingFlags.NonPublic | BindingFlags.Static);

        if (configurationMethod == null)
        {
            throw new InvalidOperationException($"could not find middleware configuration method '{nameof(ConfigureMiddleware)}'");
        }

        var genericConfigurationMethod = configurationMethod.MakeGenericMethod(middlewareType, GetMiddlewareConfigurationType(middlewareType) ?? typeof(NullObserverMiddlewareConfiguration));

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
        if (IsMiddlewareRegistered<TMiddleware, TConfiguration>(services))
        {
            return;
        }

        _ = services.AddSingleton<IEventObserverMiddlewareInvoker, EventObserverMiddlewareInvoker<TMiddleware, TConfiguration>>();
    }

    private static bool IsMiddlewareRegistered<TMiddleware, TConfiguration>(IServiceCollection services)
    {
        return services.Any(d => d.ImplementationType == typeof(EventObserverMiddlewareInvoker<TMiddleware, TConfiguration>));
    }

    private static Type? GetMiddlewareConfigurationType(Type t) => t.GetInterfaces().First(IsEventObserverMiddlewareInterface).GetGenericArguments().FirstOrDefault();

    private static bool IsEventObserverMiddlewareInterface(Type i) => i == typeof(IEventObserverMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventObserverMiddleware<>));
}
