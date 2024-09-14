using System;
using Conqueror.CQS.QueryHandling;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class InMemoryQueryTransportClientBuilderExtensions
{
    public static IQueryTransportClient UseInMemory(this IQueryTransportClientBuilder builder)
    {
        return new InMemoryQueryTransport(typeof(IQueryHandler<,>).MakeGenericType(builder.QueryType, builder.ResponseType), null);
    }

    public static IQueryTransportClient UseInMemory<TQuery, TResponse>(this IQueryTransportClientBuilder builder,
                                                                       Action<IQueryPipeline<TQuery, TResponse>> configure)
        where TQuery : class
    {
        return new InMemoryQueryTransport(typeof(IQueryHandler<,>).MakeGenericType(builder.QueryType, builder.ResponseType), configure);
    }
}
