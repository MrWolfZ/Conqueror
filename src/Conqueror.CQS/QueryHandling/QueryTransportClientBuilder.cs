using System;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryTransportClientBuilder(IServiceProvider serviceProvider, Type queryType, Type responseType) : IQueryTransportClientBuilder
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public Type QueryType { get; } = queryType;

    public Type ResponseType { get; } = responseType;
}
