using System;
using System.Collections.Generic;
using System.Threading;

namespace Conqueror;

public abstract class StreamProducerMiddlewareContext<TRequest, TItem>
    where TRequest : class
{
    public abstract TRequest Request { get; }

    public abstract CancellationToken CancellationToken { get; }

    public abstract IServiceProvider ServiceProvider { get; }

    public abstract ConquerorContext ConquerorContext { get; }

    public abstract IAsyncEnumerable<TItem> Next(TRequest request, CancellationToken cancellationToken);
}

public abstract class StreamProducerMiddlewareContext<TRequest, TItem, TConfiguration> : StreamProducerMiddlewareContext<TRequest, TItem>
    where TRequest : class
{
    public abstract TConfiguration Configuration { get; }
}
