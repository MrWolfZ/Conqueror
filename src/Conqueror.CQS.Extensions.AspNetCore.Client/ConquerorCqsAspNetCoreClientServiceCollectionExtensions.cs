using System;
using System.Linq;
using System.Net.Http;
using Conqueror;
using Conqueror.CQS.Extensions.AspNetCore.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorCqsAspNetCoreClientServiceCollectionExtensions
    {
        public static IServiceCollection AddConquerorCqsHttpClientServices(this IServiceCollection services)
        {
            services.TryAddSingleton<ConfigurationProvider>();
            services.TryAddSingleton<HttpClientFactory>();
            services.TryAddTransient<IConquerorHttpClientFactory, TransientHttpClientFactory>();

            services.TryAddSingleton<ConquerorContextAccessor>();
            services.TryAddSingleton<IConquerorContextAccessor>(p => p.GetRequiredService<ConquerorContextAccessor>());

            return services;
        }

        public static IServiceCollection AddConquerorCqsHttpClientServices(this IServiceCollection services,
                                                                           Action<ConquerorCqsHttpClientGlobalOptions> configure)
        {
            services.AddConquerorCqsHttpClientServices();
            services.AddSingleton(configure);

            return services;
        }

        public static IServiceCollection AddConquerorCommandHttpClient<TCommandHandler>(this IServiceCollection services,
                                                                                        Func<IServiceProvider, HttpClient> httpClientFactory)
            where TCommandHandler : class, ICommandHandler
        {
            return services.AddConquerorCommandHttpClient(p => p.CreateCommandHttpClient<TCommandHandler>(httpClientFactory));
        }

        public static IServiceCollection AddConquerorCommandHttpClient<TCommandHandler>(this IServiceCollection services,
                                                                                        Func<IServiceProvider, HttpClient> httpClientFactory,
                                                                                        Action<ConquerorCqsHttpClientOptions> configure)
            where TCommandHandler : class, ICommandHandler
        {
            return services.AddConquerorCommandHttpClient(p => p.CreateCommandHttpClient<TCommandHandler>(httpClientFactory, configure));
        }

        public static IServiceCollection AddConquerorCommandHttpClient<TCommandHandler>(this IServiceCollection services,
                                                                                        Func<IServiceProvider, Uri> baseAddressFactory)
            where TCommandHandler : class, ICommandHandler
        {
            return services.AddConquerorCommandHttpClient(p => p.CreateCommandHttpClient<TCommandHandler>(baseAddressFactory));
        }

        public static IServiceCollection AddConquerorCommandHttpClient<TCommandHandler>(this IServiceCollection services,
                                                                                        Func<IServiceProvider, Uri> baseAddressFactory,
                                                                                        Action<ConquerorCqsHttpClientOptions> configure)
            where TCommandHandler : class, ICommandHandler
        {
            return services.AddConquerorCommandHttpClient(p => p.CreateCommandHttpClient<TCommandHandler>(baseAddressFactory, configure));
        }

        public static IServiceCollection AddConquerorQueryHttpClient<TQueryHandler>(this IServiceCollection services,
                                                                                    Func<IServiceProvider, HttpClient> httpClientFactory)
            where TQueryHandler : class, IQueryHandler
        {
            return services.AddConquerorQueryHttpClient(p => p.CreateQueryHttpClient<TQueryHandler>(httpClientFactory));
        }

        public static IServiceCollection AddConquerorQueryHttpClient<TQueryHandler>(this IServiceCollection services,
                                                                                    Func<IServiceProvider, HttpClient> httpClientFactory,
                                                                                    Action<ConquerorCqsHttpClientOptions> configure)
            where TQueryHandler : class, IQueryHandler
        {
            return services.AddConquerorQueryHttpClient(p => p.CreateQueryHttpClient<TQueryHandler>(httpClientFactory, configure));
        }

        public static IServiceCollection AddConquerorQueryHttpClient<TQueryHandler>(this IServiceCollection services,
                                                                                    Func<IServiceProvider, Uri> baseAddressFactory)
            where TQueryHandler : class, IQueryHandler
        {
            return services.AddConquerorQueryHttpClient(p => p.CreateQueryHttpClient<TQueryHandler>(baseAddressFactory));
        }

        public static IServiceCollection AddConquerorQueryHttpClient<TQueryHandler>(this IServiceCollection services,
                                                                                    Func<IServiceProvider, Uri> baseAddressFactory,
                                                                                    Action<ConquerorCqsHttpClientOptions> configure)
            where TQueryHandler : class, IQueryHandler
        {
            return services.AddConquerorQueryHttpClient(p => p.CreateQueryHttpClient<TQueryHandler>(baseAddressFactory, configure));
        }

        private static IServiceCollection AddConquerorCommandHttpClient<TCommandHandler>(this IServiceCollection services, Func<IServiceProvider, TCommandHandler> factory)
            where TCommandHandler : class, ICommandHandler
        {
            services.AddConquerorCqsHttpClientServices();

            var (commandType, _) = typeof(TCommandHandler).GetCommandAndResponseTypes().Single();
            commandType.AssertCommandIsHttpCommand();

            var plainHandlerInterfaceType = typeof(TCommandHandler).GetCommandHandlerInterfaceTypes().Single();

            if (services.Any(d => d.ServiceType == plainHandlerInterfaceType))
            {
                throw new InvalidOperationException($"an http client for command handler {typeof(TCommandHandler).FullName} is already registered");
            }

            _ = services.AddTransient(factory);

            if (typeof(TCommandHandler).IsCustomCommandHandlerInterfaceType())
            {
                _ = services.AddTransient(plainHandlerInterfaceType, p => p.GetRequiredService<TCommandHandler>());
            }

            return services;
        }

        private static IServiceCollection AddConquerorQueryHttpClient<TQueryHandler>(this IServiceCollection services, Func<IServiceProvider, TQueryHandler> factory)
            where TQueryHandler : class, IQueryHandler
        {
            services.AddConquerorCqsHttpClientServices();

            var (queryType, _) = typeof(TQueryHandler).GetQueryAndResponseTypes().Single();
            queryType.AssertQueryIsHttpQuery();

            var plainHandlerInterfaceType = typeof(TQueryHandler).GetQueryHandlerInterfaceTypes().Single();

            if (services.Any(d => d.ServiceType == plainHandlerInterfaceType))
            {
                throw new InvalidOperationException($"an http client for query handler {typeof(TQueryHandler).FullName} is already registered");
            }

            _ = services.AddTransient(factory);

            if (typeof(TQueryHandler).IsCustomQueryHandlerInterfaceType())
            {
                _ = services.AddTransient(plainHandlerInterfaceType, p => p.GetRequiredService<TQueryHandler>());
            }

            return services;
        }
    }
}
