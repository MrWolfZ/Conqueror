using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming;

internal sealed class StreamConsumerFactory(
    IServiceProvider serviceProvider,
    StreamConsumerMiddlewareRegistry middlewareRegistry) : IStreamConsumerFactory
{
    public IStreamConsumer<TItem> Create<TItem>(Func<TItem, IServiceProvider, CancellationToken, Task> consumerFn)
    {
        var delegateConsumer = new DelegateStreamConsumer<TItem>(consumerFn, serviceProvider);
        return new StreamConsumerProxy<TItem>(serviceProvider, null, null, null, delegateConsumer, middlewareRegistry);
    }

    public IStreamConsumer<TItem> Create<TItem>(Func<TItem, IServiceProvider, CancellationToken, Task> consumerFn,
                                                Action<IStreamConsumerPipelineBuilder> configurePipeline)
    {
        var delegateConsumer = new DelegateStreamConsumer<TItem>(consumerFn, serviceProvider);
        return new StreamConsumerProxy<TItem>(serviceProvider, configurePipeline, null, null, delegateConsumer, middlewareRegistry);
    }
}
