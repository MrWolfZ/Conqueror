using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Streaming;

internal sealed class StreamProducerProxy<TRequest, TItem>(
    IServiceProvider serviceProvider,
    StreamProducerTransportClientFactory transportClientFactory,
    Action<IStreamProducerPipelineBuilder>? configurePipeline,
    StreamProducerMiddlewareRegistry producerMiddlewareRegistry)
    : IStreamProducer<TRequest, TItem>
    where TRequest : class
{
    // note that it is important for this function to be async instead of just returning the result
    // of pipeline.Execute directly, since otherwise the conqueror context will be set "too early",
    // i.e. during the creation of the enumerable instead of during the actual enumeration
    public async IAsyncEnumerable<TItem> ExecuteRequest(TRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();

        if (conquerorContext.GetExecutionTransportTypeName() is null || conquerorContext.GetStreamingRequestId() is null)
        {
            conquerorContext.SetStreamingRequestId(ActivitySpanId.CreateRandom().ToString());
        }

        var pipelineBuilder = new StreamProducerPipelineBuilder(serviceProvider, producerMiddlewareRegistry);

        configurePipeline?.Invoke(pipelineBuilder);

        var pipeline = pipelineBuilder.Build(conquerorContext);

        await foreach (var item in pipeline.Execute<TRequest, TItem>(serviceProvider, request, transportClientFactory, cancellationToken).ConfigureAwait(false))
        {
            // workaround for execution context not being automatically captured across
            // yields (see https://github.com/dotnet/runtime/issues/47802)
            var execContext = ExecutionContext.Capture();

            yield return item;

            if (execContext is not null)
            {
                ExecutionContext.Restore(execContext);
            }
        }
    }
}
