using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryTransportRegistry : IQueryTransportRegistry
{
    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<(Type QueryType, Type ResponseType, object Attribute)>> queryTypesByTransportAttribute = new();
    private readonly Dictionary<Type, QueryHandlerRegistration> registrationByQueryType;

    public QueryTransportRegistry(IEnumerable<QueryHandlerRegistration> registrations)
    {
        registrationByQueryType = registrations.ToDictionary(r => r.QueryType);
    }

    public IReadOnlyCollection<(Type QueryType, Type ResponseType, TTransportMarkerAttribute Attribute)> GetQueryTypesForTransport<TTransportMarkerAttribute>()
        where TTransportMarkerAttribute : Attribute
    {
        var entries = queryTypesByTransportAttribute.GetOrAdd(typeof(TTransportMarkerAttribute),
                                                              _ => (from r in registrationByQueryType.Values
                                                                    let attribute = r.QueryType.GetCustomAttribute<TTransportMarkerAttribute>()
                                                                    where attribute != null || typeof(TTransportMarkerAttribute) == typeof(InProcessQueryAttribute)
                                                                    select (r.QueryType, r.ResponseType, (object)attribute ?? new InProcessQueryAttribute())).ToList());

        return entries.Select(e => (e.QueryType, e.ResponseType, (TTransportMarkerAttribute)e.Attribute)).ToList();
    }

    public QueryHandlerRegistration? GetQueryHandlerRegistration(Type queryType)
    {
        return registrationByQueryType.GetValueOrDefault(queryType);
    }
}

public sealed record QueryHandlerRegistration(Type QueryType, Type ResponseType, Type HandlerType, Delegate? ConfigurePipeline);
