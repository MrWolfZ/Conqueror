using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryHandlerRegistry : IQueryHandlerRegistry
{
    private readonly IReadOnlyCollection<QueryHandlerRegistration> allRegistrations;
    private readonly Dictionary<Type, QueryHandlerRegistrationInternal> registrationByQueryType;

    public QueryHandlerRegistry(IEnumerable<QueryHandlerRegistrationInternal> registrations)
    {
        registrationByQueryType = registrations.ToDictionary(r => r.QueryType);
        allRegistrations = registrationByQueryType.Values.Select(r => new QueryHandlerRegistration(r.QueryType, r.ResponseType, r.HandlerType)).ToList();
    }

    public IReadOnlyCollection<QueryHandlerRegistration> GetQueryHandlerRegistrations() => allRegistrations;

    public QueryHandlerRegistrationInternal? GetQueryHandlerRegistration(Type queryType)
    {
        return registrationByQueryType.GetValueOrDefault(queryType);
    }
}

public sealed record QueryHandlerRegistrationInternal(Type QueryType, Type ResponseType, Type HandlerType, Delegate? ConfigurePipeline);
