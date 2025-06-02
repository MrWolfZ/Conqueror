using Conqueror;
using Conqueror.Transport.Http.Client.Messaging;
using Conqueror.Transport.Http.Client.Signalling.Sse;
using Conqueror.Transport.Http.Client.Signalling.WebSockets;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
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

    private static void AddMessaging(IServiceCollection services)
    {
        services.TryAddSingleton<IHttpMessageSenderFactory, HttpMessageSenderFactory>();
    }

    private static void AddSignalling(IServiceCollection services)
    {
        services.TryAddSingleton<IHttpSseSignalReceivers, HttpSseSignalReceivers>();
        services.TryAddSingleton<HttpSseSignalReceiversRunner>();

        services.TryAddSingleton<IHttpWebSocketsSignalReceivers, HttpWebSocketsSignalReceivers>();
        services.TryAddSingleton<HttpWebSocketsSignalReceiversRunner>();
    }
}
