using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryHandlerRegistry(IEnumerable<QueryHandlerRegistration> registrations) : IQueryHandlerRegistry
{
    private readonly IReadOnlyCollection<QueryHandlerRegistration> registrations = registrations.ToList();

    public IReadOnlyCollection<QueryHandlerRegistration> GetQueryHandlerRegistrations() => registrations;
}
