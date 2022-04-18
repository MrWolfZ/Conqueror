using System;
using System.Net.Http;
using System.Text.Json;

namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    public sealed class ConquerorHttpClientOptions
    {
        public Func<IServiceProvider, HttpClient>? HttpClientFactory { get; set; }

        public Func<IServiceProvider, JsonSerializerOptions>? JsonSerializerOptionsFactory { get; set; }

        internal Type? HandlerType { get; init; }
    }
}
