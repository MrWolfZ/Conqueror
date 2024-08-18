using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.Streaming;

internal sealed class StreamingRequestMiddlewareRegistry
{
    private readonly IReadOnlyDictionary<Type, IStreamingRequestMiddlewareInvoker> invokers;

    public StreamingRequestMiddlewareRegistry(IEnumerable<IStreamingRequestMiddlewareInvoker> invokers)
    {
        this.invokers = invokers.ToDictionary(i => i.MiddlewareType);
    }

    public IStreamingRequestMiddlewareInvoker? GetStreamingRequestMiddlewareInvoker<TMiddleware>()
        where TMiddleware : IStreamingRequestMiddlewareMarker
    {
        return invokers.GetValueOrDefault(typeof(TMiddleware));
    }
}
