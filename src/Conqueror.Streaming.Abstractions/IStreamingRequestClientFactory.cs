using System;
using System.Threading.Tasks;

namespace Conqueror;

public interface IStreamingRequestClientFactory
{
    THandler CreateStreamingRequestClient<THandler>(Func<IStreamingRequestTransportClientBuilder, IStreamingRequestTransportClient> transportClientFactory,
                                                    Action<IStreamingRequestPipelineBuilder>? configurePipeline = null)
        where THandler : class, IStreamingRequestHandler
    {
        return CreateStreamingRequestClient<THandler>(b => Task.FromResult(transportClientFactory(b)), configurePipeline);
    }

    THandler CreateStreamingRequestClient<THandler>(Func<IStreamingRequestTransportClientBuilder, Task<IStreamingRequestTransportClient>> transportClientFactory,
                                                    Action<IStreamingRequestPipelineBuilder>? configurePipeline = null)
        where THandler : class, IStreamingRequestHandler;
}
