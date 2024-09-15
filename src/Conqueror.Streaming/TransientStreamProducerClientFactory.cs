using System;
using System.Threading.Tasks;

namespace Conqueror.Streaming;

internal sealed class TransientStreamProducerClientFactory(
    StreamProducerClientFactory innerFactory,
    IServiceProvider serviceProvider)
    : IStreamProducerClientFactory
{
    public TProducer CreateStreamProducerClient<TProducer>(Func<IStreamProducerTransportClientBuilder, Task<IStreamProducerTransportClient>> transportClientFactory, Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
        where TProducer : class, IStreamProducer
    {
        return innerFactory.CreateStreamProducerClient<TProducer>(serviceProvider, transportClientFactory, configurePipeline);
    }
}
