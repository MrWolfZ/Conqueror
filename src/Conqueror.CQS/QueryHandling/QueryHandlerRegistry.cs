using System.Collections.Generic;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryHandlerRegistry : IQueryHandlerRegistry
    {
        private readonly IReadOnlyCollection<QueryHandlerRegistration> registrations;

        public QueryHandlerRegistry(IReadOnlyCollection<QueryHandlerRegistration> registrations)
        {
            this.registrations = registrations;
        }

        public IReadOnlyCollection<QueryHandlerRegistration> GetQueryHandlerRegistrations() => registrations;
    }
}
