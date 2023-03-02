using System;
using System.Text.Json;

namespace Conqueror.Streaming.Interactive.Transport.Http.Client;

public sealed class ConquerorInteractiveStreamingHttpClientOptions
{
    internal ConquerorInteractiveStreamingHttpClientOptions(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public JsonSerializerOptions? JsonSerializerOptions { get; set; }
}
