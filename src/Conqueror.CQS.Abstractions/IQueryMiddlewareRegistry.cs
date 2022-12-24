using System;
using System.Collections.Generic;

namespace Conqueror
{
    public interface IQueryMiddlewareRegistry
    {
        public IReadOnlyCollection<QueryMiddlewareRegistration> GetQueryMiddlewareRegistrations();
    }
    
    public sealed record QueryMiddlewareRegistration(Type MiddlewareType, Type? ConfigurationType);
}
