using System;
using Conqueror;
using Conqueror.CQS.Extensions.AspNetCore.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorCqsAspNetCoreClientServiceCollectionExtensions
    {
        public static IServiceCollection AddConquerorCqsHttpClientServices(this IServiceCollection services)
        {
            services.AddConquerorCQS();

            services.TryAddSingleton<ConfigurationProvider>();

            services.TryAddSingleton<ConquerorContextAccessor>();
            services.TryAddSingleton<IConquerorContextAccessor>(p => p.GetRequiredService<ConquerorContextAccessor>());

            return services;
        }

        public static IServiceCollection AddConquerorCqsHttpClientServices(this IServiceCollection services,
                                                                           Action<ConquerorCqsHttpClientGlobalOptions> configure)
        {
            services.AddConquerorCqsHttpClientServices();
            services.AddSingleton(configure);

            return services;
        }
    }
}
