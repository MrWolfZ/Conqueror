#pragma warning disable IDE0130 // Namespaces don't match folder structure - it's a convention to place service collection extensions in this namespace

namespace Microsoft.Extensions.DependencyInjection;

using System.Reflection;
using System.Runtime.ExceptionServices;
using Conqueror;
using Conqueror.Streaming;

public static class ConquerorStreamProducerMiddlewareServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorStreamProducerMiddleware<TMiddleware>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Transient
    )
        where TMiddleware : class, IStreamProducerMiddlewareMarker
    {
        return services.AddConquerorStreamProducerMiddleware(
            typeof(TMiddleware),
            new(typeof(TMiddleware), typeof(TMiddleware), lifetime)
        );
    }

    public static IServiceCollection AddConquerorStreamProducerMiddleware<TMiddleware>(
        this IServiceCollection services,
        Func<IServiceProvider, TMiddleware> factory,
        ServiceLifetime lifetime = ServiceLifetime.Transient
    )
        where TMiddleware : class, IStreamProducerMiddlewareMarker
    {
        return services.AddConquerorStreamProducerMiddleware(
            typeof(TMiddleware),
            new(typeof(TMiddleware), factory, lifetime)
        );
    }

    public static IServiceCollection AddConquerorStreamProducerMiddleware<TMiddleware>(
        this IServiceCollection services,
        TMiddleware instance
    )
        where TMiddleware : class, IStreamProducerMiddlewareMarker =>
        services.AddConquerorStreamProducerMiddleware(typeof(TMiddleware), new(typeof(TMiddleware), instance));

    public static IServiceCollection AddConquerorStreamProducerMiddleware(
        this IServiceCollection services,
        Type middlewareType,
        ServiceDescriptor serviceDescriptor
    )
    {
        if (services.Any(d => d.ServiceType == middlewareType))
        {
            return services;
        }

        services.Add(serviceDescriptor);

        return services.AddConquerorStreamProducerMiddleware(middlewareType);
    }

    internal static IServiceCollection AddConquerorStreamProducerMiddleware(
        this IServiceCollection services,
        Type middlewareType
    )
    {
        var middlewareInterfaces = middlewareType.GetInterfaces().Where(IsStreamProducerMiddlewareInterface).ToList();

        switch (middlewareInterfaces.Count)
        {
            case < 1:
                throw new ArgumentException(
                    $"type '{middlewareType.Name}' implements no stream producer middleware interface",
                    nameof(middlewareType)
                );

            case > 1:
                throw new ArgumentException(
                    $"type {middlewareType.Name} implements {nameof(IStreamProducerMiddleware)} more than once",
                    nameof(middlewareType)
                );
            default:
                // all ok
                break;
        }

        services.AddConquerorStreaming();

        var configurationMethod =
            typeof(ConquerorStreamProducerMiddlewareServiceCollectionExtensions).GetMethod(
                nameof(ConfigureMiddleware),
#pragma warning disable S3011
                BindingFlags.NonPublic | BindingFlags.Static
#pragma warning restore S3011
            )
            ?? throw new InvalidOperationException(
                $"could not find middleware configuration method '{nameof(ConfigureMiddleware)}'"
            );

        var genericConfigurationMethod = configurationMethod.MakeGenericMethod(
            middlewareType,
            GetMiddlewareConfigurationType(middlewareType) ?? typeof(NullStreamProducerMiddlewareConfiguration)
        );

        try
        {
            _ = genericConfigurationMethod.Invoke(obj: null, [services]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }

        return services;
    }

    private static void ConfigureMiddleware<TMiddleware, TConfiguration>(IServiceCollection services)
    {
        _ = services.AddSingleton<
            IStreamProducerMiddlewareInvoker,
            StreamProducerMiddlewareInvoker<TMiddleware, TConfiguration>
        >();
    }

    private static Type? GetMiddlewareConfigurationType(Type t) =>
        t.GetInterfaces().First(IsStreamProducerMiddlewareInterface).GetGenericArguments().FirstOrDefault();

    private static bool IsStreamProducerMiddlewareInterface(Type i) =>
        i == typeof(IStreamProducerMiddleware)
        || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStreamProducerMiddleware<>));
}
