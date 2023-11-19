using System;
using Conqueror.Eventing.Transport.WebSockets.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

// ReSharper disable InconsistentNaming

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorEventingTransportWebSocketsClientServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorEventingWebSocketsClient(this IServiceCollection services,
                                                                          Action<ConquerorEventingWebSocketsTransportClientGlobalOptions> configure)
    {
        services.AddConquerorEventing();

        services.AddSingleton(configure);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ConquerorEventingTransportWebSocketsClientHostedService>());

        return services;
    }

    public static IServiceCollection ConfigureConquerorEventingWebSocketsClientOptions(this IServiceCollection services,
                                                                                       Action<ConquerorEventingWebSocketsTransportClientGlobalOptions> configure)
    {
        // developer note: this method is identical to AddConquerorEventingWebSocketsClient, but the name better expresses
        // that multiple configurations are merged (i.e. calling add+configure is more intuitive than add+add, even though
        // they both do the same thing)
        return services.AddConquerorEventingWebSocketsClient(configure);
    }
}
