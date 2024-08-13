using System;
using System.Threading.Tasks;

namespace Conqueror.Streaming;

internal sealed class TransientStreamingRequestHandlerClientFactory : IStreamingRequestClientFactory
{
    private readonly StreamingRequestClientFactory innerFactory;
    private readonly IServiceProvider serviceProvider;

    public TransientStreamingRequestHandlerClientFactory(StreamingRequestClientFactory innerFactory, IServiceProvider serviceProvider)
    {
        this.innerFactory = innerFactory;
        this.serviceProvider = serviceProvider;
    }

    public THandler CreateStreamingRequestClient<THandler>(Func<IStreamingRequestTransportClientBuilder, Task<IStreamingRequestTransportClient>> transportClientFactory, Action<IStreamingRequestPipelineBuilder>? configurePipeline = null)
        where THandler : class, IStreamingRequestHandler
    {
        return innerFactory.CreateStreamingRequestClient<THandler>(serviceProvider, transportClientFactory, configurePipeline);
    }
}
