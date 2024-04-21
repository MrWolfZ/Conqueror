using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryMiddlewareRegistry
{
    private readonly IReadOnlyDictionary<Type, IQueryMiddlewareInvoker> invokers;

    public QueryMiddlewareRegistry(IEnumerable<IQueryMiddlewareInvoker> invokers)
    {
        this.invokers = invokers.ToDictionary(i => i.MiddlewareType);
    }

    public IQueryMiddlewareInvoker? GetQueryMiddlewareInvoker<TMiddleware>()
        where TMiddleware : IQueryMiddlewareMarker
    {
        return invokers.GetValueOrDefault(typeof(TMiddleware));
    }
}
