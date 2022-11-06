using System;
using Conqueror;
using Conqueror.CQS.Transport.Http.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable InconsistentNaming

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorCqsAspNetCoreClientServiceCollectionExtensions
    {
        public static IServiceCollection AddConquerorCQSHttpClientServices(this IServiceCollection services)
        {
            services.AddConquerorCQS();

            services.TryAddSingleton<ConfigurationProvider>();

            services.TryAddSingleton<ConquerorContextAccessor>();
            services.TryAddSingleton<IConquerorContextAccessor>(p => p.GetRequiredService<ConquerorContextAccessor>());

            return services;
        }

        public static IServiceCollection AddConquerorCQSHttpClientServices(this IServiceCollection services,
                                                                           Action<ConquerorCqsHttpClientGlobalOptions> configure)
        {
            services.AddConquerorCQSHttpClientServices();
            services.AddSingleton(configure);

            return services;
        }
    }
}
