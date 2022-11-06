using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.Transport.Http.Client;

public static class ConquerorHttpCommandTransportClientBuilderExtensions
{
    public static ICommandTransportClient UseHttp(this ICommandTransportClientBuilder builder, HttpClient httpClient, Action<ConquerorCqsHttpClientOptions>? configure = null)
    {
        var registration = new HttpClientRegistration
        {
            HttpClientFactory = _ => httpClient,
            BaseAddressFactory = null,
            ConfigurationAction = configure,
        };
        
        var configurationProvider = builder.ServiceProvider.GetRequiredService<ConfigurationProvider>();
        return new HttpCommandTransportClient(configurationProvider.GetOptions(builder.ServiceProvider, registration), builder.ServiceProvider.GetService<IConquerorContextAccessor>());
    }

    public static ICommandTransportClient UseHttp(this ICommandTransportClientBuilder builder, Uri baseAddress, Action<ConquerorCqsHttpClientOptions>? configure = null)
    {
        var registration = new HttpClientRegistration
        {
            HttpClientFactory = null,
            BaseAddressFactory = _ => baseAddress,
            ConfigurationAction = configure,
        };
        
        var configurationProvider = builder.ServiceProvider.GetRequiredService<ConfigurationProvider>();
        return new HttpCommandTransportClient(configurationProvider.GetOptions(builder.ServiceProvider, registration), builder.ServiceProvider.GetService<IConquerorContextAccessor>());
    }
}
