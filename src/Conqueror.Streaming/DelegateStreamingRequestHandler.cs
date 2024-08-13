using System;
using System.Collections.Generic;
using System.Threading;

namespace Conqueror.Streaming;

internal sealed class DelegateStreamingRequestHandler<TRequest, TItem> : IStreamingRequestHandler<TRequest, TItem>
    where TRequest : class
{
    private readonly Func<TRequest, IServiceProvider, CancellationToken, IAsyncEnumerable<TItem>> handlerFn;
    private readonly IServiceProvider serviceProvider;

    public DelegateStreamingRequestHandler(Func<TRequest, IServiceProvider, CancellationToken, IAsyncEnumerable<TItem>> handlerFn,
                                           IServiceProvider serviceProvider)
    {
        this.handlerFn = handlerFn;
        this.serviceProvider = serviceProvider;
    }

    public IAsyncEnumerable<TItem> ExecuteRequest(TRequest request, CancellationToken cancellationToken = default)
    {
        return handlerFn(request, serviceProvider, cancellationToken);
    }
}
