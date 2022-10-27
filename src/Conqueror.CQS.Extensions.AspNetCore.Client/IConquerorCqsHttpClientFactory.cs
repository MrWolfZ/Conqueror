using System;
using System.Net.Http;

namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    public interface IConquerorCqsHttpClientFactory
    {
        THandler CreateQueryHttpClient<THandler>(Func<IServiceProvider, HttpClient> httpClientFactory)
            where THandler : class, IQueryHandler;
        
        THandler CreateQueryHttpClient<THandler>(Func<IServiceProvider, HttpClient> httpClientFactory, Action<ConquerorCqsHttpClientOptions> configure)
            where THandler : class, IQueryHandler;
        
        THandler CreateQueryHttpClient<THandler>(Func<IServiceProvider, Uri> baseAddressFactory)
            where THandler : class, IQueryHandler;
        
        THandler CreateQueryHttpClient<THandler>(Func<IServiceProvider, Uri> baseAddressFactory, Action<ConquerorCqsHttpClientOptions> configure)
            where THandler : class, IQueryHandler;
    }
}
