using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Streaming;

internal sealed class StreamConsumerProxy<TItem>(
    IServiceProvider serviceProvider,
    Action<IStreamConsumerPipelineBuilder>? configurePipeline,
    Type? consumerType,
    object? key,
    IStreamConsumer<TItem>? consumerInstance,
    StreamConsumerMiddlewareRegistry middlewareRegistry)
    : IStreamConsumer<TItem>
{
    public Task HandleItem(TItem item, CancellationToken cancellationToken = default)
    {
        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();

        var pipelineBuilder = new StreamConsumerPipelineBuilder(serviceProvider, middlewareRegistry);

        configurePipeline?.Invoke(pipelineBuilder);

        var pipeline = pipelineBuilder.Build(conquerorContext);

        return pipeline.Execute(serviceProvider, item, consumerType, key, consumerInstance, cancellationToken);
    }
}
