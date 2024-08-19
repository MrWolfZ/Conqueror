using System;
using System.Collections.Generic;
using System.Threading;

namespace Conqueror.Streaming;

internal sealed class DelegateStreamProducer<TRequest, TItem> : IStreamProducer<TRequest, TItem>
    where TRequest : class
{
    private readonly Func<TRequest, IServiceProvider, CancellationToken, IAsyncEnumerable<TItem>> producerFn;
    private readonly IServiceProvider serviceProvider;

    public DelegateStreamProducer(Func<TRequest, IServiceProvider, CancellationToken, IAsyncEnumerable<TItem>> producerFn,
                                  IServiceProvider serviceProvider)
    {
        this.producerFn = producerFn;
        this.serviceProvider = serviceProvider;
    }

    public IAsyncEnumerable<TItem> ExecuteRequest(TRequest request, CancellationToken cancellationToken = default)
    {
        return producerFn(request, serviceProvider, cancellationToken);
    }
}
