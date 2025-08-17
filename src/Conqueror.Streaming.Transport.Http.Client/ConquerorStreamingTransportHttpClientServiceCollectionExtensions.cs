#pragma warning disable IDE0130 // Namespaces don't match folder structure - it's a convention to place service collection extensions in this namespace

namespace Microsoft.Extensions.DependencyInjection;

using Conqueror.Streaming.Transport.Http.Client;
using Extensions;

public static class ConquerorStreamingTransportHttpClientServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorStreamingHttpClientServices(this IServiceCollection services)
    {
        services.AddConquerorStreaming();

        services.TryAddSingleton<ConfigurationProvider>();

        return services;
    }

    public static IServiceCollection AddConquerorStreamingHttpClientServices(
        this IServiceCollection services,
        Action<ConquerorStreamingHttpClientGlobalOptions> configure
    )
    {
        AddConquerorStreamingHttpClientServices(services);
        services.AddSingleton(configure);

        return services;
    }

    public static IServiceCollection ConfigureConquerorCqsHttpClientOptions(
        this IServiceCollection services,
        Action<ConquerorStreamingHttpClientGlobalOptions> configure
    )
    {
        // developer note: this method is identical to AddConquerorStreamingHttpClientServices, but the name better expresses
        // that multiple configurations are merged (i.e. calling add+configure is more intuitive than add+add, even though
        // they both do the same thing)
        return services.AddConquerorStreamingHttpClientServices(configure);
    }
}
