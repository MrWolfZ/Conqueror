using System;
using Conqueror;
using Conqueror.Eventing.Transport.WebSockets.Server.AspNetCore;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorEventingTransportWebSocketsServerAspNetCoreServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorEventingWebSocketsTransportPublisher(this IServiceCollection services,
                                                                                      Action<IEventPublisherPipelineBuilder>? configurePipeline = null)
    {
        services.AddConquerorEventTransportPublisher<WebSocketsTransportPublisher>(ServiceLifetime.Singleton, configurePipeline);

        return services;
    }
}
