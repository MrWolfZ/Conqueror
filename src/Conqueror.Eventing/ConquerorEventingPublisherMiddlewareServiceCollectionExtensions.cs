using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Conqueror;
using Conqueror.Eventing;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorEventingPublisherMiddlewareServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorEventPublisherMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                       ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TMiddleware : class, IEventPublisherMiddlewareMarker
    {
        return services.AddConquerorEventPublisherMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), typeof(TMiddleware), lifetime));
    }

    public static IServiceCollection AddConquerorEventPublisherMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                       Func<IServiceProvider, TMiddleware> factory,
                                                                                       ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TMiddleware : class, IEventPublisherMiddlewareMarker
    {
        return services.AddConquerorEventPublisherMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), factory, lifetime));
    }

    public static IServiceCollection AddConquerorEventPublisherMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                       TMiddleware instance)
        where TMiddleware : class, IEventPublisherMiddlewareMarker
    {
        return services.AddConquerorEventPublisherMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), instance));
    }

    internal static IServiceCollection AddConquerorEventPublisherMiddleware(this IServiceCollection services,
                                                                            Type middlewareType,
                                                                            ServiceDescriptor serviceDescriptor)
    {
        services.AddConquerorEventing();

        services.Replace(serviceDescriptor);

        return services.AddConquerorEventPublisherMiddleware(middlewareType);
    }

    internal static bool IsEventPublisherMiddlewareRegistered(this IServiceCollection services, Type middlewareType)
    {
        var registrationCheckMethod = typeof(ConquerorEventingPublisherMiddlewareServiceCollectionExtensions).GetMethod(nameof(IsMiddlewareRegistered), BindingFlags.NonPublic | BindingFlags.Static);

        if (registrationCheckMethod == null)
        {
            throw new InvalidOperationException($"could not find middleware registration check method '{nameof(IsMiddlewareRegistered)}'");
        }

        var genericRegistrationCheckMethod = registrationCheckMethod.MakeGenericMethod(middlewareType, GetMiddlewareConfigurationType(middlewareType) ?? typeof(NullPublisherMiddlewareConfiguration));

        try
        {
            return (bool)genericRegistrationCheckMethod.Invoke(null, [services])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // unreachable code that is necessary so that the compiler knows the catch throws
        }
    }

    private static IServiceCollection AddConquerorEventPublisherMiddleware(this IServiceCollection services,
                                                                           Type middlewareType)
    {
        var middlewareInterfaces = middlewareType.GetInterfaces().Where(IsEventPublisherMiddlewareInterface).ToList();

        switch (middlewareInterfaces.Count)
        {
            case < 1:
                throw new ArgumentException($"type '{middlewareType.Name}' implements no publisher middleware interface");

            case > 1:
                throw new ArgumentException($"type {middlewareType.Name} implements {nameof(IEventPublisherMiddleware)} more than once");
        }

        var configurationMethod = typeof(ConquerorEventingPublisherMiddlewareServiceCollectionExtensions).GetMethod(nameof(ConfigureMiddleware), BindingFlags.NonPublic | BindingFlags.Static);

        if (configurationMethod == null)
        {
            throw new InvalidOperationException($"could not find middleware configuration method '{nameof(ConfigureMiddleware)}'");
        }

        var genericConfigurationMethod = configurationMethod.MakeGenericMethod(middlewareType, GetMiddlewareConfigurationType(middlewareType) ?? typeof(NullPublisherMiddlewareConfiguration));

        try
        {
            _ = genericConfigurationMethod.Invoke(null, [services]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }

        return services;
    }

    private static void ConfigureMiddleware<TMiddleware, TConfiguration>(IServiceCollection services)
    {
        if (IsMiddlewareRegistered<TMiddleware, TConfiguration>(services))
        {
            return;
        }

        _ = services.AddSingleton<IEventPublisherMiddlewareInvoker, EventPublisherMiddlewareInvoker<TMiddleware, TConfiguration>>();
    }

    private static bool IsMiddlewareRegistered<TMiddleware, TConfiguration>(IServiceCollection services)
    {
        return services.Any(d => d.ImplementationType == typeof(EventPublisherMiddlewareInvoker<TMiddleware, TConfiguration>));
    }

    private static Type? GetMiddlewareConfigurationType(Type t) => t.GetInterfaces().First(IsEventPublisherMiddlewareInterface).GetGenericArguments().FirstOrDefault();

    private static bool IsEventPublisherMiddlewareInterface(Type i) => i == typeof(IEventPublisherMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventPublisherMiddleware<>));
}
