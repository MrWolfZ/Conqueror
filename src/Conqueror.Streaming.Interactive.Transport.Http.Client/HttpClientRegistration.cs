using System;

namespace Conqueror.Streaming.Interactive.Transport.Http.Client
{
    internal sealed class HttpClientRegistration
    {
        public HttpClientRegistration(Type handlerType, Func<IServiceProvider, Uri> baseAddressFactory)
        {
            HandlerType = handlerType;
            BaseAddressFactory = baseAddressFactory;
        }

        public Type HandlerType { get; }

        public Func<IServiceProvider, Uri> BaseAddressFactory { get; }
        
        public Action<ConquerorInteractiveStreamingHttpClientOptions>? ConfigurationAction { get; init; }
    }
}
