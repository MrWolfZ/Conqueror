using System;
using System.Linq;
using Conqueror;
using Conqueror.Streaming.Transport.Http.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorStreamingAspNetCoreClientServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorStreamingHttpClientServices(this IServiceCollection services)
    {
        services.AddConquerorStreaming();

        services.TryAddSingleton<ConfigurationProvider>();
        services.TryAddSingleton<HttpClientFactory>();
        services.TryAddTransient<IConquerorStreamingHttpClientFactory, TransientHttpClientFactory>();

        return services;
    }

    public static IServiceCollection AddConquerorStreamingHttpClientServices(this IServiceCollection services,
                                                                             Action<ConquerorStreamingHttpClientGlobalOptions> configure)
    {
        services.AddConquerorStreamingHttpClientServices();
        services.AddSingleton(configure);

        return services;
    }

    public static IServiceCollection AddConquerorStreamingHttpClient<TStreamingHandler>(this IServiceCollection services,
                                                                                        Func<IServiceProvider, Uri> baseAddressFactory)
        where TStreamingHandler : class, IStreamingHandler
    {
        return services.AddConquerorStreamingHttpClient(p => p.CreateStreamingHttpClient<TStreamingHandler>(baseAddressFactory));
    }

    public static IServiceCollection AddConquerorStreamingHttpClient<TStreamingHandler>(this IServiceCollection services,
                                                                                        Func<IServiceProvider, Uri> baseAddressFactory,
                                                                                        Action<ConquerorStreamingHttpClientOptions> configure)
        where TStreamingHandler : class, IStreamingHandler
    {
        return services.AddConquerorStreamingHttpClient(p => p.CreateStreamingHttpClient<TStreamingHandler>(baseAddressFactory, configure));
    }

    private static IServiceCollection AddConquerorStreamingHttpClient<TStreamingHandler>(this IServiceCollection services, Func<IServiceProvider, TStreamingHandler> factory)
        where TStreamingHandler : class, IStreamingHandler
    {
        services.AddConquerorStreamingHttpClientServices();

        var (requestType, _) = typeof(TStreamingHandler).GetStreamingRequestAndItemTypes().Single();
        requestType.AssertRequestIsHttpStream();

        var plainHandlerInterfaceType = typeof(TStreamingHandler).GetStreamingHandlerInterfaceTypes().Single();

        if (services.Any(d => d.ServiceType == plainHandlerInterfaceType))
        {
            throw new InvalidOperationException($"an http client for query handler {typeof(TStreamingHandler).FullName} is already registered");
        }

        _ = services.AddTransient(factory);

        if (typeof(TStreamingHandler).IsCustomStreamingHandlerInterfaceType())
        {
            _ = services.AddTransient(plainHandlerInterfaceType, p => p.GetRequiredService<TStreamingHandler>());
        }

        return services;
    }
}
