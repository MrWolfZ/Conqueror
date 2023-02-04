using System;
using System.Linq;
using Conqueror;
using Conqueror.Streaming.Interactive.Transport.Http.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorInteractiveStreamingAspNetCoreClientServiceCollectionExtensions
    {
        public static IServiceCollection AddConquerorInteractiveStreamingHttpClientServices(this IServiceCollection services)
        {
            services.AddConquerorInteractiveStreaming();

            services.TryAddSingleton<ConfigurationProvider>();
            services.TryAddSingleton<HttpClientFactory>();
            services.TryAddTransient<IConquerorInteractiveStreamingHttpClientFactory, TransientHttpClientFactory>();

            return services;
        }

        public static IServiceCollection AddConquerorInteractiveStreamingHttpClientServices(this IServiceCollection services,
                                                                                            Action<ConquerorInteractiveStreamingHttpClientGlobalOptions> configure)
        {
            services.AddConquerorInteractiveStreamingHttpClientServices();
            services.AddSingleton(configure);

            return services;
        }

        public static IServiceCollection AddConquerorInteractiveStreamingHttpClient<TStreamingHandler>(this IServiceCollection services,
                                                                                                       Func<IServiceProvider, Uri> baseAddressFactory)
            where TStreamingHandler : class, IInteractiveStreamingHandler
        {
            return services.AddConquerorInteractiveStreamingHttpClient(p => p.CreateInteractiveStreamingHttpClient<TStreamingHandler>(baseAddressFactory));
        }

        public static IServiceCollection AddConquerorInteractiveStreamingHttpClient<TStreamingHandler>(this IServiceCollection services,
                                                                                                       Func<IServiceProvider, Uri> baseAddressFactory,
                                                                                                       Action<ConquerorInteractiveStreamingHttpClientOptions> configure)
            where TStreamingHandler : class, IInteractiveStreamingHandler
        {
            return services.AddConquerorInteractiveStreamingHttpClient(p => p.CreateInteractiveStreamingHttpClient<TStreamingHandler>(baseAddressFactory, configure));
        }

        private static IServiceCollection AddConquerorInteractiveStreamingHttpClient<TStreamingHandler>(this IServiceCollection services, Func<IServiceProvider, TStreamingHandler> factory)
            where TStreamingHandler : class, IInteractiveStreamingHandler
        {
            services.AddConquerorInteractiveStreamingHttpClientServices();

            var (requestType, _) = typeof(TStreamingHandler).GetInteractiveStreamingRequestAndItemTypes().Single();
            requestType.AssertRequestIsHttpInteractiveStream();

            var plainHandlerInterfaceType = typeof(TStreamingHandler).GetInteractiveStreamingHandlerInterfaceTypes().Single();

            if (services.Any(d => d.ServiceType == plainHandlerInterfaceType))
            {
                throw new InvalidOperationException($"an http client for query handler {typeof(TStreamingHandler).FullName} is already registered");
            }

            _ = services.AddTransient(factory);

            if (typeof(TStreamingHandler).IsCustomInteractiveStreamingHandlerInterfaceType())
            {
                _ = services.AddTransient(plainHandlerInterfaceType, p => p.GetRequiredService<TStreamingHandler>());
            }

            return services;
        }
    }
}
