using System;
using Conqueror.CQS.Transport.Http.Client;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class HttpCommandTransportClientBuilderExtensions
{
    public static ICommandTransportClient UseHttp(this ICommandTransportClientBuilder builder, Uri baseAddress, Action<HttpCommandClientOptions>? configure = null)
    {
        baseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));

        var registration = new HttpClientRegistration
        {
            BaseAddress = baseAddress,
            CommandConfigurationAction = configure,
            CommandType = builder.CommandType,
        };

        var configurationProvider = builder.ServiceProvider.GetRequiredService<ConfigurationProvider>();
        return new HttpCommandTransportClient(configurationProvider.GetOptions(builder.ServiceProvider, registration),
                                              builder.ServiceProvider.GetRequiredService<IConquerorContextAccessor>());
    }
}
