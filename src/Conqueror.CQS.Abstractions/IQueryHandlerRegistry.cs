using System;
using System.Collections.Generic;

namespace Conqueror;

public interface IQueryHandlerRegistry
{
    public IReadOnlyCollection<QueryHandlerRegistration> GetQueryHandlerRegistrations();

    QueryHandlerRegistration? GetQueryHandlerRegistration(Type queryType, Type responseType);
}

public sealed record QueryHandlerRegistration(Type QueryType, Type ResponseType, Type HandlerType, Delegate? ConfigurePipeline);
