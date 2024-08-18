using System;
using Conqueror.Streaming.Transport.Http.Client;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class HttpStreamingRequestTransportClientBuilderExtensions
{
    public static IStreamingRequestTransportClient UseWebSocket(this IStreamingRequestTransportClientBuilder builder, Uri baseAddress, Action<HttpStreamClientOptions>? configure = null)
    {
        baseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));

        var registration = new HttpClientRegistration
        {
            BaseAddress = baseAddress,
            StreamConfigurationAction = configure,
            RequestType = builder.RequestType,
        };

        var configurationProvider = builder.ServiceProvider.GetRequiredService<ConfigurationProvider>();
        return new HttpStreamingRequestTransportClient(configurationProvider.GetOptions(builder.ServiceProvider, registration),
                                                       builder.ServiceProvider.GetRequiredService<IConquerorContextAccessor>());
    }
}
