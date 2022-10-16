using System;
using System.Text.Json;

namespace Conqueror.Streaming.Interactive.Extensions.AspNetCore.Client
{
    public sealed class ConquerorInteractiveStreamingHttpClientOptions
    {
        internal ConquerorInteractiveStreamingHttpClientOptions(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public JsonSerializerOptions? JsonSerializerOptions { get; set; }
    }
}
