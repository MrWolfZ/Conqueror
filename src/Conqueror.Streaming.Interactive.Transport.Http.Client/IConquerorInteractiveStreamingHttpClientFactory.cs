using System;

namespace Conqueror.Streaming.Interactive.Transport.Http.Client;

public interface IConquerorInteractiveStreamingHttpClientFactory
{
    THandler CreateInteractiveStreamingHttpClient<THandler>(Func<IServiceProvider, Uri> baseAddressFactory)
        where THandler : class, IInteractiveStreamingHandler;

    THandler CreateInteractiveStreamingHttpClient<THandler>(Func<IServiceProvider, Uri> baseAddressFactory, Action<ConquerorInteractiveStreamingHttpClientOptions> configure)
        where THandler : class, IInteractiveStreamingHandler;
}
