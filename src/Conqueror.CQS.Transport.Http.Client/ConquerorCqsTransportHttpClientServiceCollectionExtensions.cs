using System;
using Conqueror.CQS.Transport.Http.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorCqsTransportHttpClientServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorCQSHttpClientServices(this IServiceCollection services)
    {
        services.AddConquerorCQS();

        services.TryAddSingleton<ConfigurationProvider>();

        return services;
    }

    public static IServiceCollection AddConquerorCQSHttpClientServices(this IServiceCollection services,
                                                                       Action<ConquerorCqsHttpClientGlobalOptions> configure)
    {
        services.AddConquerorCQSHttpClientServices();
        services.AddSingleton(configure);

        return services;
    }

    public static IServiceCollection ConfigureConquerorCQSHttpClientOptions(this IServiceCollection services,
                                                                            Action<ConquerorCqsHttpClientGlobalOptions> configure)
    {
        // developer note: this method is identical to AddConquerorCQSHttpClientServices, but the name better expresses
        // that multiple configurations are merged (i.e. calling add+configure is more intuitive than add+add, even though
        // they both do the same thing)
        return services.AddConquerorCQSHttpClientServices(configure);
    }
}
