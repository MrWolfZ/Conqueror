using Conqueror.Transport.Http.Client.Signalling.Sse;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorHttpClientServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorHttpClient(this IServiceCollection services)
    {
        _ = services.AddConqueror();

        services.TryAddSingleton<HttpSseSignalReceiversRunner>();

        return services;
    }
}
