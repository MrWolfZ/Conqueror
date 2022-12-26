using System;
using System.Text.Json;

namespace Conqueror.CQS.Transport.Http.Client
{
    public sealed class HttpCommandClientOptions
    {
        internal HttpCommandClientOptions(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public JsonSerializerOptions? JsonSerializerOptions { get; set; }

        public IHttpCommandPathConvention? PathConvention { get; set; }
    }
}
