using System;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryTransportClientBuilder : IQueryTransportClientBuilder
{
    public QueryTransportClientBuilder(IServiceProvider serviceProvider, Type queryType, Type responseType)
    {
        ServiceProvider = serviceProvider;
        QueryType = queryType;
        ResponseType = responseType;
    }

    public IServiceProvider ServiceProvider { get; }

    public Type QueryType { get; }

    public Type ResponseType { get; }
}
