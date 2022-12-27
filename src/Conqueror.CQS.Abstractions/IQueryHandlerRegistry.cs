using System;
using System.Collections.Generic;

namespace Conqueror
{
    public interface IQueryHandlerRegistry
    {
        public IReadOnlyCollection<QueryHandlerRegistration> GetQueryHandlerRegistrations();
    }
    
    public sealed record QueryHandlerRegistration(Type QueryType, Type ResponseType, Type HandlerType);
}
