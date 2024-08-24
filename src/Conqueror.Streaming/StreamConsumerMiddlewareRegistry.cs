using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.Streaming;

internal sealed class StreamConsumerMiddlewareRegistry
{
    private readonly IReadOnlyDictionary<Type, IStreamConsumerMiddlewareInvoker> invokers;

    public StreamConsumerMiddlewareRegistry(IEnumerable<IStreamConsumerMiddlewareInvoker> invokers)
    {
        this.invokers = invokers.ToDictionary(i => i.MiddlewareType);
    }

    public IStreamConsumerMiddlewareInvoker? GetStreamConsumerMiddlewareInvoker<TMiddleware>()
        where TMiddleware : IStreamConsumerMiddlewareMarker
    {
        return invokers.GetValueOrDefault(typeof(TMiddleware));
    }
}
