using System;
using System.Collections.Generic;
using System.Threading;

namespace Conqueror.Streaming;

internal sealed class DelegateStreamProducer<TRequest, TItem>(
    Func<TRequest, IServiceProvider, CancellationToken, IAsyncEnumerable<TItem>> producerFn,
    IServiceProvider serviceProvider)
    : IStreamProducer<TRequest, TItem>
    where TRequest : class
{
    public IAsyncEnumerable<TItem> ExecuteRequest(TRequest request, CancellationToken cancellationToken = default)
    {
        return producerFn(request, serviceProvider, cancellationToken);
    }
}
