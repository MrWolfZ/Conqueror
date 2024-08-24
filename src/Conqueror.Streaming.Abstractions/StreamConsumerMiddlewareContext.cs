using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror;

public abstract class StreamConsumerMiddlewareContext<TItem>
{
    public abstract TItem Item { get; }

    public abstract CancellationToken CancellationToken { get; }

    public abstract IServiceProvider ServiceProvider { get; }

    public abstract IConquerorContext ConquerorContext { get; }

    public abstract Task Next(TItem item, CancellationToken cancellationToken);
}

public abstract class StreamConsumerMiddlewareContext<TItem, TConfiguration> : StreamConsumerMiddlewareContext<TItem>
{
    public abstract TConfiguration Configuration { get; }
}
