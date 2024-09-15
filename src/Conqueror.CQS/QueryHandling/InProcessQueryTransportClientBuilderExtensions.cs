using System;
using Conqueror.CQS.QueryHandling;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class InProcessQueryTransportClientBuilderExtensions
{
    public static IQueryTransportClient UseInProcess(this IQueryTransportClientBuilder builder)
    {
        return new InProcessQueryTransport(typeof(IQueryHandler<,>).MakeGenericType(builder.QueryType, builder.ResponseType), null);
    }

    public static IQueryTransportClient UseInProcess<TQuery, TResponse>(this IQueryTransportClientBuilder builder,
                                                                        Action<IQueryPipeline<TQuery, TResponse>> configure)
        where TQuery : class
    {
        return new InProcessQueryTransport(typeof(IQueryHandler<,>).MakeGenericType(builder.QueryType, builder.ResponseType), configure);
    }
}
