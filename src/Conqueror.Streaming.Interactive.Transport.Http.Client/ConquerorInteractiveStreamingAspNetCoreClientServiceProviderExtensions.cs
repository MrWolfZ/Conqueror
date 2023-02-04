using System;
using Conqueror;
using Conqueror.Streaming.Interactive.Transport.Http.Client;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ConquerorInteractiveStreamingAspNetCoreClientServiceProviderExtensions
    {
        public static TStreamingHandler CreateInteractiveStreamingHttpClient<TStreamingHandler>(this IServiceProvider provider,
                                                                                                Func<IServiceProvider, Uri> baseAddressFactory)
            where TStreamingHandler : class, IInteractiveStreamingHandler
        {
            return provider.GetRequiredService<IConquerorInteractiveStreamingHttpClientFactory>().CreateInteractiveStreamingHttpClient<TStreamingHandler>(baseAddressFactory);
        }

        public static TStreamingHandler CreateInteractiveStreamingHttpClient<TStreamingHandler>(this IServiceProvider provider,
                                                                                                Func<IServiceProvider, Uri> baseAddressFactory,
                                                                                                Action<ConquerorInteractiveStreamingHttpClientOptions> configure)
            where TStreamingHandler : class, IInteractiveStreamingHandler
        {
            return provider.GetRequiredService<IConquerorInteractiveStreamingHttpClientFactory>().CreateInteractiveStreamingHttpClient<TStreamingHandler>(baseAddressFactory, configure);
        }
    }
}
