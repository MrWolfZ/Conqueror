using System;
using System.Net.Http;
using Conqueror.CQS.Transport.Http.Client;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror
{
    public static class HttpCommandTransportClientBuilderExtensions
    {
        public static ICommandTransportClient UseHttp(this ICommandTransportClientBuilder builder, HttpClient httpClient, Action<HttpCommandClientOptions>? configure = null)
        {
            var registration = new HttpClientRegistration
            {
                HttpClientFactory = _ => httpClient,
                CommandConfigurationAction = configure,
            };
        
            var configurationProvider = builder.ServiceProvider.GetRequiredService<ConfigurationProvider>();
            return new HttpCommandTransportClient(configurationProvider.GetOptions(builder.ServiceProvider, registration), builder.ServiceProvider.GetService<IConquerorContextAccessor>());
        }

        public static ICommandTransportClient UseHttp(this ICommandTransportClientBuilder builder, Uri baseAddress, Action<HttpCommandClientOptions>? configure = null)
        {
            var registration = new HttpClientRegistration
            {
                BaseAddressFactory = _ => baseAddress,
                CommandConfigurationAction = configure,
            };
        
            var configurationProvider = builder.ServiceProvider.GetRequiredService<ConfigurationProvider>();
            return new HttpCommandTransportClient(configurationProvider.GetOptions(builder.ServiceProvider, registration), builder.ServiceProvider.GetService<IConquerorContextAccessor>());
        }
    }
}
