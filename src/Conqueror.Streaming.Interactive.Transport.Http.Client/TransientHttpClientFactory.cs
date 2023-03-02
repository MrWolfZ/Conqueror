using System;

namespace Conqueror.Streaming.Interactive.Transport.Http.Client;

internal sealed class TransientHttpClientFactory : IConquerorInteractiveStreamingHttpClientFactory
{
    private readonly HttpClientFactory innerFactory;
    private readonly IServiceProvider serviceProvider;

    public TransientHttpClientFactory(HttpClientFactory innerFactory, IServiceProvider serviceProvider)
    {
        this.innerFactory = innerFactory;
        this.serviceProvider = serviceProvider;
    }

    public THandler CreateInteractiveStreamingHttpClient<THandler>(Func<IServiceProvider, Uri> baseAddressFactory)
        where THandler : class, IInteractiveStreamingHandler
    {
        return innerFactory.CreateInteractiveStreamingHttpClient<THandler>(serviceProvider, baseAddressFactory);
    }

    public THandler CreateInteractiveStreamingHttpClient<THandler>(Func<IServiceProvider, Uri> baseAddressFactory, Action<ConquerorInteractiveStreamingHttpClientOptions> configure)
        where THandler : class, IInteractiveStreamingHandler
    {
        return innerFactory.CreateInteractiveStreamingHttpClient<THandler>(serviceProvider, baseAddressFactory, configure);
    }
}
