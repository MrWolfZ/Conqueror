using System;
using System.Net.WebSockets;

namespace Conqueror.Streaming.Transport.Http.Client;

internal sealed class ConfigurationProvider
{
    private readonly Action<ConquerorStreamingHttpClientGlobalOptions>? configureGlobalOptions;

    public ConfigurationProvider(Action<ConquerorStreamingHttpClientGlobalOptions>? configureGlobalOptions = null)
    {
        this.configureGlobalOptions = configureGlobalOptions;
    }

    public ResolvedHttpClientOptions GetOptions(IServiceProvider provider, HttpClientRegistration registration)
    {
        var globalOptions = new ConquerorStreamingHttpClientGlobalOptions(provider);
        configureGlobalOptions?.Invoke(globalOptions);

        var clientOptions = new ConquerorStreamingHttpClientOptions(provider);
        registration.ConfigurationAction?.Invoke(clientOptions);

        var webSocketClientFactory = globalOptions.WebSocketFactory ?? (async (address, cancellationToken) =>
        {
            var socket = new ClientWebSocket();

            await socket.ConnectAsync(address, cancellationToken).ConfigureAwait(false);

            return socket;
        });

        var jsonSerializerOptions = clientOptions.JsonSerializerOptions ?? globalOptions.JsonSerializerOptions;

        return new(webSocketClientFactory, registration.BaseAddressFactory.Invoke(provider), jsonSerializerOptions);
    }
}
