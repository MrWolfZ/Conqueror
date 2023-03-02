using System;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryTransportClientBuilder : IQueryTransportClientBuilder
{
    public QueryTransportClientBuilder(IServiceProvider serviceProvider, Type queryType)
    {
        ServiceProvider = serviceProvider;
        QueryType = queryType;
    }

    public IServiceProvider ServiceProvider { get; }

    public Type QueryType { get; }
}
