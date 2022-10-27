using System;
using System.Net.Http;

namespace Conqueror.CQS.Extensions.AspNetCore.Client
{
    internal sealed class TransientHttpClientFactory : IConquerorCqsHttpClientFactory
    {
        private readonly HttpClientFactory innerFactory;
        private readonly IServiceProvider serviceProvider;

        public TransientHttpClientFactory(HttpClientFactory innerFactory, IServiceProvider serviceProvider)
        {
            this.innerFactory = innerFactory;
            this.serviceProvider = serviceProvider;
        }

        public TQueryHandler CreateQueryHttpClient<TQueryHandler>(Func<IServiceProvider, HttpClient> httpClientFactory)
            where TQueryHandler : class, IQueryHandler
        {
            return innerFactory.CreateQueryHttpClient<TQueryHandler>(serviceProvider, httpClientFactory);
        }

        public TQueryHandler CreateQueryHttpClient<TQueryHandler>(Func<IServiceProvider, HttpClient> httpClientFactory, Action<ConquerorCqsHttpClientOptions> configure)
            where TQueryHandler : class, IQueryHandler
        {
            return innerFactory.CreateQueryHttpClient<TQueryHandler>(serviceProvider, httpClientFactory, configureOptions: configure);
        }

        public TQueryHandler CreateQueryHttpClient<TQueryHandler>(Func<IServiceProvider, Uri> baseAddressFactory)
            where TQueryHandler : class, IQueryHandler
        {
            return innerFactory.CreateQueryHttpClient<TQueryHandler>(serviceProvider, baseAddressFactory: baseAddressFactory);
        }

        public TQueryHandler CreateQueryHttpClient<TQueryHandler>(Func<IServiceProvider, Uri> baseAddressFactory, Action<ConquerorCqsHttpClientOptions> configure)
            where TQueryHandler : class, IQueryHandler
        {
            return innerFactory.CreateQueryHttpClient<TQueryHandler>(serviceProvider, baseAddressFactory: baseAddressFactory, configureOptions: configure);
        }
    }
}
