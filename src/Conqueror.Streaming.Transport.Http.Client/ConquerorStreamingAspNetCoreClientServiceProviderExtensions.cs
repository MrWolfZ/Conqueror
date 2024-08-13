using System;
using Conqueror;
using Conqueror.Streaming.Transport.Http.Client;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorStreamingAspNetCoreClientServiceProviderExtensions
{
    public static TStreamingHandler CreateStreamingHttpClient<TStreamingHandler>(this IServiceProvider provider,
                                                                                 Func<IServiceProvider, Uri> baseAddressFactory)
        where TStreamingHandler : class, IStreamingHandler
    {
        return provider.GetRequiredService<IConquerorStreamingHttpClientFactory>().CreateStreamingHttpClient<TStreamingHandler>(baseAddressFactory);
    }

    public static TStreamingHandler CreateStreamingHttpClient<TStreamingHandler>(this IServiceProvider provider,
                                                                                 Func<IServiceProvider, Uri> baseAddressFactory,
                                                                                 Action<ConquerorStreamingHttpClientOptions> configure)
        where TStreamingHandler : class, IStreamingHandler
    {
        return provider.GetRequiredService<IConquerorStreamingHttpClientFactory>().CreateStreamingHttpClient<TStreamingHandler>(baseAddressFactory, configure);
    }
}
