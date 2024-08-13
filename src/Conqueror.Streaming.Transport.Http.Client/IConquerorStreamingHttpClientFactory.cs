using System;

namespace Conqueror.Streaming.Transport.Http.Client;

public interface IConquerorStreamingHttpClientFactory
{
    THandler CreateStreamingHttpClient<THandler>(Func<IServiceProvider, Uri> baseAddressFactory)
        where THandler : class, IStreamingHandler;

    THandler CreateStreamingHttpClient<THandler>(Func<IServiceProvider, Uri> baseAddressFactory, Action<ConquerorStreamingHttpClientOptions> configure)
        where THandler : class, IStreamingHandler;
}
