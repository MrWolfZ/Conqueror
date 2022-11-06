using System;
using System.Text.Json;

namespace Conqueror.CQS.Transport.Http.Client
{
    public sealed class ConquerorCqsHttpClientOptions
    {
        internal ConquerorCqsHttpClientOptions(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public JsonSerializerOptions? JsonSerializerOptions { get; set; }
    }
}
