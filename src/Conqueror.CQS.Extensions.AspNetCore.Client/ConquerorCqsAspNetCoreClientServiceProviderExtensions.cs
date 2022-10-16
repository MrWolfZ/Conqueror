using System;
using System.Net.Http;
using Conqueror;
using Conqueror.CQS.Extensions.AspNetCore.Client;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorCqsAspNetCoreClientServiceProviderExtensions
    {
        public static TCommandHandler CreateCommandHttpClient<TCommandHandler>(this IServiceProvider provider,
                                                                               Func<IServiceProvider, HttpClient> httpClientFactory)
            where TCommandHandler : class, ICommandHandler
        {
            return provider.GetRequiredService<IConquerorCqsHttpClientFactory>().CreateCommandHttpClient<TCommandHandler>(httpClientFactory);
        }

        public static TCommandHandler CreateCommandHttpClient<TCommandHandler>(this IServiceProvider provider,
                                                                               Func<IServiceProvider, HttpClient> httpClientFactory,
                                                                               Action<ConquerorCqsHttpClientOptions> configure)
            where TCommandHandler : class, ICommandHandler
        {
            return provider.GetRequiredService<IConquerorCqsHttpClientFactory>().CreateCommandHttpClient<TCommandHandler>(httpClientFactory, configure);
        }

        public static TCommandHandler CreateCommandHttpClient<TCommandHandler>(this IServiceProvider provider,
                                                                               Func<IServiceProvider, Uri> baseAddressFactory)
            where TCommandHandler : class, ICommandHandler
        {
            return provider.GetRequiredService<IConquerorCqsHttpClientFactory>().CreateCommandHttpClient<TCommandHandler>(baseAddressFactory);
        }

        public static TCommandHandler CreateCommandHttpClient<TCommandHandler>(this IServiceProvider provider,
                                                                               Func<IServiceProvider, Uri> baseAddressFactory,
                                                                               Action<ConquerorCqsHttpClientOptions> configure)
            where TCommandHandler : class, ICommandHandler
        {
            return provider.GetRequiredService<IConquerorCqsHttpClientFactory>().CreateCommandHttpClient<TCommandHandler>(baseAddressFactory, configure);
        }

        public static TQueryHandler CreateQueryHttpClient<TQueryHandler>(this IServiceProvider provider,
                                                                         Func<IServiceProvider, HttpClient> httpClientFactory)
            where TQueryHandler : class, IQueryHandler
        {
            return provider.GetRequiredService<IConquerorCqsHttpClientFactory>().CreateQueryHttpClient<TQueryHandler>(httpClientFactory);
        }

        public static TQueryHandler CreateQueryHttpClient<TQueryHandler>(this IServiceProvider provider,
                                                                         Func<IServiceProvider, HttpClient> httpClientFactory,
                                                                         Action<ConquerorCqsHttpClientOptions> configure)
            where TQueryHandler : class, IQueryHandler
        {
            return provider.GetRequiredService<IConquerorCqsHttpClientFactory>().CreateQueryHttpClient<TQueryHandler>(httpClientFactory, configure);
        }

        public static TQueryHandler CreateQueryHttpClient<TQueryHandler>(this IServiceProvider provider,
                                                                         Func<IServiceProvider, Uri> baseAddressFactory)
            where TQueryHandler : class, IQueryHandler
        {
            return provider.GetRequiredService<IConquerorCqsHttpClientFactory>().CreateQueryHttpClient<TQueryHandler>(baseAddressFactory);
        }

        public static TQueryHandler CreateQueryHttpClient<TQueryHandler>(this IServiceProvider provider,
                                                                         Func<IServiceProvider, Uri> baseAddressFactory,
                                                                         Action<ConquerorCqsHttpClientOptions> configure)
            where TQueryHandler : class, IQueryHandler
        {
            return provider.GetRequiredService<IConquerorCqsHttpClientFactory>().CreateQueryHttpClient<TQueryHandler>(baseAddressFactory, configure);
        }
    }
}
