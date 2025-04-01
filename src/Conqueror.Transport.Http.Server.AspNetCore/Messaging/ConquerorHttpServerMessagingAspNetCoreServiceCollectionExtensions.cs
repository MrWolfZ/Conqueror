using Conqueror.Transport.Http.Server.AspNetCore.Messaging;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorHttpServerMessagingAspNetCoreServiceCollectionExtensions
{
    public static IServiceCollection AddMessageEndpoints(this IServiceCollection services)
    {
        _ = services.AddConqueror();

        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IApiDescriptionProvider, ConquerorHttpServerMessagingEndpointMetadataApiDescriptionProvider>());

        return services;
    }
}
