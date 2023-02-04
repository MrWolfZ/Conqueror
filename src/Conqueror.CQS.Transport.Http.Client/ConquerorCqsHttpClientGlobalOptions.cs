using System;
using System.Net.Http;
using System.Text.Json;

namespace Conqueror.CQS.Transport.Http.Client
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

        public IHttpCommandPathConvention? CommandPathConvention { get; set; }

        public IHttpQueryPathConvention? QueryPathConvention { get; set; }
    }
}
