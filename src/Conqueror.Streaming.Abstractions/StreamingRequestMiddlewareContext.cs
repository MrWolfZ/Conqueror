using System;
using System.Collections.Generic;
using System.Threading;

namespace Conqueror;

public abstract class StreamingRequestMiddlewareContext<TRequest, TItem>
    where TRequest : class
{
    public abstract TRequest Request { get; }

    public abstract CancellationToken CancellationToken { get; }

    public abstract IServiceProvider ServiceProvider { get; }

    public abstract IConquerorContext ConquerorContext { get; }

    public abstract IAsyncEnumerable<TItem> Next(TRequest command, CancellationToken cancellationToken);
}

public abstract class StreamingRequestMiddlewareContext<TRequest, TItem, TConfiguration> : StreamingRequestMiddlewareContext<TRequest, TItem>
    where TRequest : class
{
    public abstract TConfiguration Configuration { get; }
}
