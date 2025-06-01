using Conqueror;
using Conqueror.Transport.Http.Server.AspNetCore.Messaging;
using Conqueror.Transport.Http.Server.AspNetCore.Signalling.Sse;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorHttpServerAspNetCoreServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorHttpServerAspNetCore(this IServiceCollection services)
    {
        _ = services.AddConqueror()
                    .AddEndpointsApiExplorer();

        AddMessaging(services);
        AddSignalling(services);

        return services;
    }

    private static void AddMessaging(IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IApiDescriptionProvider, ConquerorHttpServerMessagingEndpointMetadataApiDescriptionProvider>());
    }

    private static void AddSignalling(IServiceCollection services)
    {
        services.TryAddSingleton<IHttpSseSignalPublisherFactory, HttpSseSignalPublisherFactory>();
        services.TryAddSingleton<HttpSseSignalBroker>();
    }
}
