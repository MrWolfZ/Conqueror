#pragma warning disable IDE0130 // Namespaces don't match folder structure - it's a convention to place service collection extensions in this namespace

namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorHttpClientServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorHttpClient(this IServiceCollection services)
    {
        _ = services.AddConqueror();

        AddMessaging(services);
        AddSignalling(services);

        return services;
    }

    private static void AddMessaging(IServiceCollection services) =>
        services.TryAddSingleton<IHttpMessageSenderFactory, HttpMessageSenderFactory>();

    private static void AddSignalling(IServiceCollection services)
    {
        services.TryAddSingleton<IHttpSseSignalReceivers, HttpSseSignalReceivers>();
        services.TryAddSingleton<HttpSseSignalReceiverFactory>();
        services.TryAddSingleton<HttpSseSignalReceiverRunner>();

        services.TryAddSingleton<IHttpWebSocketsSignalReceivers, HttpWebSocketsSignalReceivers>();
        services.TryAddSingleton<HttpWebSocketsSignalReceiverFactory>();
        services.TryAddSingleton<HttpWebSocketsSignalReceiverRunner>();
    }
}
