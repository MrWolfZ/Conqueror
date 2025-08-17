#pragma warning disable IDE0130 // Namespaces don't match folder structure - we want these extensions to be accessible from client registration code without an extra import

namespace Conqueror;

using Microsoft.Extensions.DependencyInjection;
using Streaming.Transport.Http.Client;

public static class HttpStreamProducerTransportClientBuilderExtensions
{
    public static IStreamProducerTransportClient UseWebSocket(
        this IStreamProducerTransportClientBuilder builder,
        Uri baseAddress,
        Action<HttpStreamClientOptions>? configure = null
    )
    {
        baseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));

        var registration = new HttpClientRegistration
        {
            BaseAddress = baseAddress,
            StreamConfigurationAction = configure,
            RequestType = builder.RequestType,
        };

        var configurationProvider = builder.ServiceProvider.GetRequiredService<ConfigurationProvider>();

        return new HttpStreamProducerTransportClient(
            configurationProvider.GetOptions(builder.ServiceProvider, registration),
            builder.ServiceProvider.GetRequiredService<IConquerorContextAccessor>()
        );
    }
}
