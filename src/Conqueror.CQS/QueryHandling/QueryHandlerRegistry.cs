using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryHandlerRegistry(IEnumerable<QueryHandlerRegistration> registrations) : IQueryHandlerRegistry
{
    private readonly Dictionary<(Type QueryType, Type ResponseType), QueryHandlerRegistration> registrations
        = registrations.ToDictionary(r => (r.QueryType, r.ResponseType));

    public IReadOnlyCollection<QueryHandlerRegistration> GetQueryHandlerRegistrations() => registrations.Values.ToList();

    public QueryHandlerRegistration? GetQueryHandlerRegistration(Type queryType, Type responseType)
    {
        return registrations.GetValueOrDefault((queryType, responseType));
    }
}
