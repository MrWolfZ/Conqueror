using System;
using System.Net.Http;
using Conqueror.CQS.Transport.Http.Client;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror
{
    public static class HttpQueryTransportClientBuilderExtensions
    {
        public static IQueryTransportClient UseHttp(this IQueryTransportClientBuilder builder, HttpClient httpClient, Action<HttpQueryClientOptions>? configure = null)
        {
            var registration = new HttpClientRegistration
            {
                HttpClientFactory = _ => httpClient,
                QueryConfigurationAction = configure,
            };
        
            var configurationProvider = builder.ServiceProvider.GetRequiredService<ConfigurationProvider>();
            return new HttpQueryTransportClient(configurationProvider.GetOptions(builder.ServiceProvider, registration), builder.ServiceProvider.GetService<IConquerorContextAccessor>());
        }

        public static IQueryTransportClient UseHttp(this IQueryTransportClientBuilder builder, Uri baseAddress, Action<HttpQueryClientOptions>? configure = null)
        {
            var registration = new HttpClientRegistration
            {
                BaseAddressFactory = _ => baseAddress,
                QueryConfigurationAction = configure,
            };
        
            var configurationProvider = builder.ServiceProvider.GetRequiredService<ConfigurationProvider>();
            return new HttpQueryTransportClient(configurationProvider.GetOptions(builder.ServiceProvider, registration), builder.ServiceProvider.GetService<IConquerorContextAccessor>());
        }
    }
}
