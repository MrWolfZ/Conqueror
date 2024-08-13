using System;

namespace Conqueror.Streaming.Transport.Http.Client;

internal sealed class TransientHttpClientFactory : IConquerorStreamingHttpClientFactory
{
    private readonly HttpClientFactory innerFactory;
    private readonly IServiceProvider serviceProvider;

    public TransientHttpClientFactory(HttpClientFactory innerFactory, IServiceProvider serviceProvider)
    {
        this.innerFactory = innerFactory;
        this.serviceProvider = serviceProvider;
    }

    public THandler CreateStreamingHttpClient<THandler>(Func<IServiceProvider, Uri> baseAddressFactory)
        where THandler : class, IStreamingRequestHandler
    {
        return innerFactory.CreateStreamingHttpClient<THandler>(serviceProvider, baseAddressFactory);
    }

    public THandler CreateStreamingHttpClient<THandler>(Func<IServiceProvider, Uri> baseAddressFactory, Action<ConquerorStreamingHttpClientOptions> configure)
        where THandler : class, IStreamingRequestHandler
    {
        return innerFactory.CreateStreamingHttpClient<THandler>(serviceProvider, baseAddressFactory, configure);
    }
}
