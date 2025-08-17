namespace Conqueror.Streaming;

internal sealed class StreamConsumerProxy<TItem>(
    IServiceProvider serviceProvider,
    Action<IStreamConsumerPipelineBuilder>? configurePipeline,
    Type? consumerType,
    object? key,
    IStreamConsumer<TItem>? consumerInstance,
    StreamConsumerMiddlewareRegistry middlewareRegistry
) : IStreamConsumer<TItem>
{
    public async Task HandleItem(TItem item, CancellationToken cancellationToken = default)
    {
        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();

        var pipelineBuilder = new StreamConsumerPipelineBuilder(serviceProvider, middlewareRegistry);

        configurePipeline?.Invoke(pipelineBuilder);

        var pipeline = pipelineBuilder.Build(conquerorContext);

        await pipeline.Execute(serviceProvider, item, consumerType, key, consumerInstance, cancellationToken);
    }
}
