using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Conqueror;
using Conqueror.CQS.CommandHandling;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorCqsCommandMiddlewareServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorCommandMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TMiddleware : class, ICommandMiddlewareMarker
    {
        return services.AddConquerorCommandMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), typeof(TMiddleware), lifetime));
    }

    public static IServiceCollection AddConquerorCommandMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                Func<IServiceProvider, TMiddleware> factory,
                                                                                ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TMiddleware : class, ICommandMiddlewareMarker
    {
        return services.AddConquerorCommandMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), factory, lifetime));
    }

    public static IServiceCollection AddConquerorCommandMiddleware<TMiddleware>(this IServiceCollection services,
                                                                                TMiddleware instance)
        where TMiddleware : class, ICommandMiddlewareMarker
    {
        return services.AddConquerorCommandMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), instance));
    }

    public static IServiceCollection AddConquerorCommandMiddleware(this IServiceCollection services,
                                                                   Type middlewareType,
                                                                   ServiceDescriptor serviceDescriptor)
    {
        if (services.Any(d => d.ServiceType == middlewareType))
        {
            return services;
        }

        services.Add(serviceDescriptor);
        return services.AddConquerorCommandMiddleware(middlewareType);
    }

    internal static IServiceCollection AddConquerorCommandMiddleware(this IServiceCollection services,
                                                                     Type middlewareType)
    {
        var middlewareInterfaces = middlewareType.GetInterfaces().Where(IsCommandMiddlewareInterface).ToList();

        switch (middlewareInterfaces.Count)
        {
            case < 1:
                throw new ArgumentException($"type '{middlewareType.Name}' implements no command middleware interface");

            case > 1:
                throw new ArgumentException($"type '{middlewareType.Name}' implements {nameof(ICommandMiddleware)} more than once");
        }

        services.AddConquerorCqsCommandServices();

        var configurationMethod = typeof(ConquerorCqsCommandMiddlewareServiceCollectionExtensions).GetMethod(nameof(ConfigureMiddleware), BindingFlags.NonPublic | BindingFlags.Static);

        if (configurationMethod == null)
        {
            throw new InvalidOperationException($"could not find middleware configuration method '{nameof(ConfigureMiddleware)}'");
        }

        var genericConfigurationMethod = configurationMethod.MakeGenericMethod(middlewareType, GetMiddlewareConfigurationType(middlewareType) ?? typeof(NullMiddlewareConfiguration));

        try
        {
            _ = genericConfigurationMethod.Invoke(null, new object[] { services });
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }

        return services;
    }

    private static void ConfigureMiddleware<TMiddleware, TConfiguration>(IServiceCollection services)
    {
        _ = services.AddSingleton<ICommandMiddlewareInvoker, CommandMiddlewareInvoker<TMiddleware, TConfiguration>>();
    }

    private static Type? GetMiddlewareConfigurationType(Type t) => t.GetInterfaces().First(IsCommandMiddlewareInterface).GetGenericArguments().FirstOrDefault();

    private static bool IsCommandMiddlewareInterface(Type i) => i == typeof(ICommandMiddleware) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandMiddleware<>));
}
