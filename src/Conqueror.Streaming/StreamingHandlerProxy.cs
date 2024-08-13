using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Streaming;

internal sealed class StreamingHandlerProxy<TRequest, TItem> : IStreamingHandler<TRequest, TItem>
    where TRequest : class
{
    // TODO: private readonly StreamingMiddlewaresInvoker invoker;
    private readonly StreamingHandlerRegistry registry;
    private readonly IServiceProvider serviceProvider;

    public StreamingHandlerProxy(StreamingHandlerRegistry registry,
                                 //// TODO: StreamingMiddlewaresInvoker invoker,
                                 IServiceProvider serviceProvider)
    {
        this.registry = registry;
        //// this.invoker = invoker;
        this.serviceProvider = serviceProvider;
    }

    public IAsyncEnumerable<TItem> ExecuteRequest(TRequest request, CancellationToken cancellationToken)
    {
        var metadata = registry.GetStreamingHandlerMetadata<TRequest, TItem>();
        var handler = serviceProvider.GetRequiredService(metadata.HandlerType) as IStreamingHandler<TRequest, TItem>;
        return handler!.ExecuteRequest(request, cancellationToken);

        //// return invoker.InvokeMiddlewares<TRequest, TItem>(serviceProvider, metadata, request, cancellationToken);
    }
}
