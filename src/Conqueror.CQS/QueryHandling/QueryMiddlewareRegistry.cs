using System.Collections.Generic;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryMiddlewareRegistry : IQueryMiddlewareRegistry
    {
        private readonly IReadOnlyCollection<QueryMiddlewareRegistration> registrations;

        public QueryMiddlewareRegistry(IReadOnlyCollection<QueryMiddlewareRegistration> registrations)
        {
            this.registrations = registrations;
        }

        public IReadOnlyCollection<QueryMiddlewareRegistration> GetQueryMiddlewareRegistrations() => registrations;
    }
}
