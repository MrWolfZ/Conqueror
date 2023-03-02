using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryHandlerRegistry : IQueryHandlerRegistry
{
    private readonly IReadOnlyCollection<QueryHandlerRegistration> registrations;

    public QueryHandlerRegistry(IEnumerable<QueryHandlerRegistration> registrations)
    {
        this.registrations = registrations.ToList();
    }

    public IReadOnlyCollection<QueryHandlerRegistration> GetQueryHandlerRegistrations() => registrations;
}
