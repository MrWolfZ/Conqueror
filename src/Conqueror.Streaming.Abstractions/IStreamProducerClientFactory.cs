using System;
using System.Threading.Tasks;

namespace Conqueror;

public interface IStreamProducerClientFactory
{
    THandler CreateStreamProducerClient<THandler>(Func<IStreamProducerTransportClientBuilder, IStreamProducerTransportClient> transportClientFactory,
                                                  Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
        where THandler : class, IStreamProducer
    {
        return CreateStreamProducerClient<THandler>(b => Task.FromResult(transportClientFactory(b)), configurePipeline);
    }

    THandler CreateStreamProducerClient<THandler>(Func<IStreamProducerTransportClientBuilder, Task<IStreamProducerTransportClient>> transportClientFactory,
                                                  Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
        where THandler : class, IStreamProducer;
}
