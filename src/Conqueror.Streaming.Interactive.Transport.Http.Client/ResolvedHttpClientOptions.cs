using System;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming.Interactive.Transport.Http.Client
{
    internal sealed class ResolvedHttpClientOptions
    {
        public ResolvedHttpClientOptions(Func<Uri, CancellationToken, Task<WebSocket>> webSocketFactory, Uri baseAddress, JsonSerializerOptions? jsonSerializerOptions)
        {
            JsonSerializerOptions = jsonSerializerOptions;
            WebSocketFactory = webSocketFactory;
            BaseAddress = baseAddress;
        }

        public Func<Uri, CancellationToken, Task<WebSocket>> WebSocketFactory { get; }

        public Uri BaseAddress { get; }

        public JsonSerializerOptions? JsonSerializerOptions { get; }
    }
}
