using System;
using System.Threading.Tasks;

namespace Conqueror.Streaming;

internal sealed class TransientStreamProducerClientFactory : IStreamProducerClientFactory
{
    private readonly StreamProducerClientFactory innerFactory;
    private readonly IServiceProvider serviceProvider;

    public TransientStreamProducerClientFactory(StreamProducerClientFactory innerFactory, IServiceProvider serviceProvider)
    {
        this.innerFactory = innerFactory;
        this.serviceProvider = serviceProvider;
    }

    public TProducer CreateStreamProducerClient<TProducer>(Func<IStreamProducerTransportClientBuilder, Task<IStreamProducerTransportClient>> transportClientFactory, Action<IStreamProducerPipelineBuilder>? configurePipeline = null)
        where TProducer : class, IStreamProducer
    {
        return innerFactory.CreateStreamProducerClient<TProducer>(serviceProvider, transportClientFactory, configurePipeline);
    }
}
