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

        public TCommandHandler CreateCommandHttpClient<TCommandHandler>(Func<IServiceProvider, HttpClient> httpClientFactory)
            where TCommandHandler : class, ICommandHandler
        {
            return innerFactory.CreateCommandHttpClient<TCommandHandler>(serviceProvider, httpClientFactory);
        }

        public TCommandHandler CreateCommandHttpClient<TCommandHandler>(Func<IServiceProvider, HttpClient> httpClientFactory, Action<ConquerorCqsHttpClientOptions> configure)
            where TCommandHandler : class, ICommandHandler
        {
            return innerFactory.CreateCommandHttpClient<TCommandHandler>(serviceProvider, httpClientFactory, configureOptions: configure);
        }

        public TCommandHandler CreateCommandHttpClient<TCommandHandler>(Func<IServiceProvider, Uri> baseAddressFactory)
            where TCommandHandler : class, ICommandHandler
        {
            return innerFactory.CreateCommandHttpClient<TCommandHandler>(serviceProvider, baseAddressFactory: baseAddressFactory);
        }

        public TCommandHandler CreateCommandHttpClient<TCommandHandler>(Func<IServiceProvider, Uri> baseAddressFactory, Action<ConquerorCqsHttpClientOptions> configure)
            where TCommandHandler : class, ICommandHandler
        {
            return innerFactory.CreateCommandHttpClient<TCommandHandler>(serviceProvider, baseAddressFactory: baseAddressFactory, configureOptions: configure);
        }
    }
}
