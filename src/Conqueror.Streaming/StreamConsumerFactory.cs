namespace Conqueror.Streaming;

internal sealed class StreamConsumerFactory(
    IServiceProvider serviceProvider,
    StreamConsumerMiddlewareRegistry middlewareRegistry
) : IStreamConsumerFactory
{
    public IStreamConsumer<TItem> Create<TItem>(Func<TItem, IServiceProvider, CancellationToken, Task> consumerFn)
    {
        var delegateConsumer = new DelegateStreamConsumer<TItem>(consumerFn, serviceProvider);

        return new StreamConsumerProxy<TItem>(
            serviceProvider,
            configurePipeline: null,
            consumerType: null,
            key: null,
            delegateConsumer,
            middlewareRegistry
        );
    }

    public IStreamConsumer<TItem> Create<TItem>(
        Func<TItem, IServiceProvider, CancellationToken, Task> consumerFn,
        Action<IStreamConsumerPipelineBuilder> configurePipeline
    )
    {
        var delegateConsumer = new DelegateStreamConsumer<TItem>(consumerFn, serviceProvider);

        return new StreamConsumerProxy<TItem>(
            serviceProvider,
            configurePipeline,
            consumerType: null,
            key: null,
            delegateConsumer,
            middlewareRegistry
        );
    }
}
