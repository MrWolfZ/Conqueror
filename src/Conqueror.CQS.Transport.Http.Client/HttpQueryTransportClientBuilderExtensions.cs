using System;
using Conqueror.CQS.Transport.Http.Client;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror
{
    public static class HttpQueryTransportClientBuilderExtensions
    {
        public static IQueryTransportClient UseHttp(this IQueryTransportClientBuilder builder, Uri baseAddress, Action<HttpQueryClientOptions>? configure = null)
        {
            baseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));

            var registration = new HttpClientRegistration
            {
                BaseAddress = baseAddress,
                QueryConfigurationAction = configure,
                QueryType = builder.QueryType,
            };

            var configurationProvider = builder.ServiceProvider.GetRequiredService<ConfigurationProvider>();
            return new HttpQueryTransportClient(configurationProvider.GetOptions(builder.ServiceProvider, registration),
                                                builder.ServiceProvider.GetRequiredService<IConquerorContextAccessor>(),
                                                builder.ServiceProvider.GetRequiredService<IQueryContextAccessor>());
        }
    }
}
