using System;
using System.Net.Http;
using System.Text.Json;

namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    public sealed class ConquerorCqsHttpClientGlobalOptions
    {
        internal ConquerorCqsHttpClientGlobalOptions(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public Func<Uri, HttpClient> HttpClientFactory { get; set; } = baseAddress => new() { BaseAddress = baseAddress };

        public JsonSerializerOptions? JsonSerializerOptions { get; set; }
    }
}
