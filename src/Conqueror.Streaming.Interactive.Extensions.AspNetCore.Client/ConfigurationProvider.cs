using System;
using System.Net.WebSockets;

namespace Conqueror.Streaming.Interactive.Extensions.AspNetCore.Client
{
    internal sealed class ConfigurationProvider
    {
        private readonly Action<ConquerorInteractiveStreamingHttpClientGlobalOptions>? configureGlobalOptions;

        public ConfigurationProvider(Action<ConquerorInteractiveStreamingHttpClientGlobalOptions>? configureGlobalOptions = null)
        {
            this.configureGlobalOptions = configureGlobalOptions;
        }

        public ResolvedHttpClientOptions GetOptions(IServiceProvider provider, HttpClientRegistration registration)
        {
            var globalOptions = new ConquerorInteractiveStreamingHttpClientGlobalOptions(provider);
            configureGlobalOptions?.Invoke(globalOptions);

            var clientOptions = new ConquerorInteractiveStreamingHttpClientOptions(provider);
            registration.ConfigurationAction?.Invoke(clientOptions);
            
            var webSocketClientFactory = globalOptions.WebSocketFactory ?? (async (address, cancellationToken) =>
            {
                var socket = new ClientWebSocket();

                await socket.ConnectAsync(address, cancellationToken);

                return socket;
            });

            var jsonSerializerOptions = clientOptions.JsonSerializerOptions ?? globalOptions.JsonSerializerOptions;

            return new(webSocketClientFactory, registration.BaseAddressFactory.Invoke(provider), jsonSerializerOptions);
        }
    }
}
