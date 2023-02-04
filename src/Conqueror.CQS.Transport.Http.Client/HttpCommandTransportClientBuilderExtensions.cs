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
                HttpClient = httpClient,
                CommandConfigurationAction = configure,
            };

            return builder.UseHttp(registration);
        }

        public static ICommandTransportClient UseHttp(this ICommandTransportClientBuilder builder, Uri baseAddress, Action<HttpCommandClientOptions>? configure = null)
        {
            var registration = new HttpClientRegistration
            {
                BaseAddress = baseAddress,
                CommandConfigurationAction = configure,
            };

            return builder.UseHttp(registration);
        }

        private static ICommandTransportClient UseHttp(this ICommandTransportClientBuilder builder, HttpClientRegistration registration)
        {
            var configurationProvider = builder.ServiceProvider.GetRequiredService<ConfigurationProvider>();
            return new HttpCommandTransportClient(configurationProvider.GetOptions(builder.ServiceProvider, registration),
                                                  builder.ServiceProvider.GetRequiredService<IConquerorContextAccessor>(),
                                                  builder.ServiceProvider.GetRequiredService<ICommandContextAccessor>());
        }
    }
}
