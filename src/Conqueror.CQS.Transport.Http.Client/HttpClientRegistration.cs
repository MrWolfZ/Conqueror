using System;
using System.Net.Http;

namespace Conqueror.CQS.Transport.Http.Client
{
    internal sealed class HttpClientRegistration
    {
        public Action<HttpCommandClientOptions>? CommandConfigurationAction { get; init; }
        
        public Action<HttpQueryClientOptions>? QueryConfigurationAction { get; init; }
        
        public Func<IServiceProvider, HttpClient>? HttpClientFactory { get; init; }
        
        public Func<IServiceProvider, Uri>? BaseAddressFactory { get; init; }
    }
}
