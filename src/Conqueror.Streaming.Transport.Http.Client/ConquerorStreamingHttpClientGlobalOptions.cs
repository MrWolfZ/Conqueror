using System;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming.Transport.Http.Client;

public sealed class ConquerorStreamingHttpClientGlobalOptions
{
    internal ConquerorStreamingHttpClientGlobalOptions(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public Func<Uri, CancellationToken, Task<WebSocket>>? WebSocketFactory { get; set; }

    public JsonSerializerOptions? JsonSerializerOptions { get; set; }
}
