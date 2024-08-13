using System;
using System.Text.Json;

namespace Conqueror.Streaming.Transport.Http.Client;

public sealed class ConquerorStreamingHttpClientOptions
{
    internal ConquerorStreamingHttpClientOptions(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public JsonSerializerOptions? JsonSerializerOptions { get; set; }
}
