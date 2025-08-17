#pragma warning disable IDE0130 // Namespaces don't match folder structure - it's a convention to place service collection extensions in this namespace

namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorHttpServerAspNetCoreServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorHttpServerAspNetCore(this IServiceCollection services)
    {
        _ = services.AddConqueror().AddEndpointsApiExplorer();

        AddMessaging(services);
        AddSignalling(services);

        return services;
    }

    private static void AddMessaging(IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Transient<
                IApiDescriptionProvider,
                ConquerorHttpServerMessagingEndpointMetadataApiDescriptionProvider
            >()
        );
    }

    private static void AddSignalling(IServiceCollection services)
    {
        services.TryAddSingleton<IHttpSseSignalPublisherFactory, HttpSseSignalPublisherFactory>();
        services.TryAddSingleton<HttpSseSignalBroker>();

        services.TryAddSingleton<IHttpWebSocketsSignalPublisherFactory, HttpWebSocketsSignalPublisherFactory>();
        services.TryAddSingleton<HttpWebSocketsSignalBroker>();
    }
}
