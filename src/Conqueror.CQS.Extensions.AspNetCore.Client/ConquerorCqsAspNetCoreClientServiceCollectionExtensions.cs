using Conqueror.CQS;
using Conqueror.CQS.Extensions.AspNetCore.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorCqsAspNetCoreClientServiceCollectionExtensions
    {
        public static IConquerorHttpClientsBuilder AddConquerorHttpClients(this IServiceCollection services)
        {
            services.TryAddSingleton<ConfigurationProvider>();
            services.TryAddTransient<IConquerorHttpClientFactory, ConquerorHttpClientFactory>();
            
            services.TryAddSingleton<ConquerorContextAccessor>();
            services.TryAddSingleton<IConquerorContextAccessor>(p => p.GetRequiredService<ConquerorContextAccessor>());

            return new ConquerorHttpClientsBuilder(services);
        }
    }
}
