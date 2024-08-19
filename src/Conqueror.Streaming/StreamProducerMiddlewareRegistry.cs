using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.Streaming;

internal sealed class StreamProducerMiddlewareRegistry
{
    private readonly IReadOnlyDictionary<Type, IStreamProducerMiddlewareInvoker> invokers;

    public StreamProducerMiddlewareRegistry(IEnumerable<IStreamProducerMiddlewareInvoker> invokers)
    {
        this.invokers = invokers.ToDictionary(i => i.MiddlewareType);
    }

    public IStreamProducerMiddlewareInvoker? GetStreamProducerMiddlewareInvoker<TMiddleware>()
        where TMiddleware : IStreamProducerMiddlewareMarker
    {
        return invokers.GetValueOrDefault(typeof(TMiddleware));
    }
}
