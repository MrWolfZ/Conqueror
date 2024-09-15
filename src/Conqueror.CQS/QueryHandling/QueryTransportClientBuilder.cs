using System;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryTransportClientBuilder(
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext,
    Type queryType,
    Type responseType)
    : IQueryTransportClientBuilder
{
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ConquerorContext ConquerorContext { get; } = conquerorContext;

    public Type QueryType { get; } = queryType;

    public Type ResponseType { get; } = responseType;
}
