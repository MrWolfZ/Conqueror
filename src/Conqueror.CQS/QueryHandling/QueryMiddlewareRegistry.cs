using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryMiddlewareRegistry : IQueryMiddlewareRegistry
{
    private readonly IReadOnlyDictionary<Type, IQueryMiddlewareInvoker> invokers;
    private readonly IReadOnlyCollection<QueryMiddlewareRegistration> registrations;

    public QueryMiddlewareRegistry(IEnumerable<QueryMiddlewareRegistration> registrations, IEnumerable<IQueryMiddlewareInvoker> invokers)
    {
        this.registrations = registrations.ToList();
        this.invokers = invokers.ToDictionary(i => i.MiddlewareType);
    }

    public IReadOnlyCollection<QueryMiddlewareRegistration> GetQueryMiddlewareRegistrations() => registrations;

    internal IReadOnlyDictionary<Type, IQueryMiddlewareInvoker> GetQueryMiddlewareInvokers() => invokers;
}
