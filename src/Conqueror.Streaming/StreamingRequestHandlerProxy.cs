using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Conqueror.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Streaming;

internal sealed class StreamingRequestHandlerProxy<TRequest, TItem> : IStreamingRequestHandler<TRequest, TItem>
    where TRequest : class
{
    private readonly Action<IStreamingRequestPipelineBuilder>? configurePipeline;
    private readonly StreamingRequestMiddlewareRegistry requestMiddlewareRegistry;
    private readonly IServiceProvider serviceProvider;
    private readonly StreamingRequestTransportClientFactory transportClientFactory;

    public StreamingRequestHandlerProxy(IServiceProvider serviceProvider,
                                        StreamingRequestTransportClientFactory transportClientFactory,
                                        Action<IStreamingRequestPipelineBuilder>? configurePipeline,
                                        StreamingRequestMiddlewareRegistry requestMiddlewareRegistry)
    {
        this.serviceProvider = serviceProvider;
        this.transportClientFactory = transportClientFactory;
        this.configurePipeline = configurePipeline;
        this.requestMiddlewareRegistry = requestMiddlewareRegistry;
    }

    public IAsyncEnumerable<TItem> ExecuteRequest(TRequest request, CancellationToken cancellationToken = default)
    {
        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();

        if (!conquerorContext.IsExecutionFromTransport() || conquerorContext.GetStreamingRequestId() is null)
        {
            conquerorContext.SetStreamingRequestId(ActivitySpanId.CreateRandom().ToString());
        }

        var pipelineBuilder = new StreamingRequestPipelineBuilder(serviceProvider, requestMiddlewareRegistry);

        configurePipeline?.Invoke(pipelineBuilder);

        var pipeline = pipelineBuilder.Build(conquerorContext);

        return pipeline.Execute<TRequest, TItem>(serviceProvider, request, transportClientFactory, cancellationToken);
    }
}
