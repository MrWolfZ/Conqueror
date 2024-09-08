using System;
using Conqueror.CQS.QueryHandling;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible without an extra import)
namespace Conqueror;

public static class ConquerorCqsQueryClientExtensions
{
    public static IQueryHandler<TQuery, TResponse> WithPipeline<TQuery, TResponse>(this IQueryHandler<TQuery, TResponse> handler,
                                                                                   Action<IQueryPipeline<TQuery, TResponse>> configurePipeline)
        where TQuery : class
    {
        if (handler is QueryHandlerProxy<TQuery, TResponse> proxy)
        {
            return proxy.WithPipeline(configurePipeline);
        }

        if (handler is QueryHandlerGeneratedProxyBase<TQuery, TResponse> generatedProxy)
        {
            return generatedProxy.WithPipeline(configurePipeline);
        }

        throw new NotSupportedException($"handler type {handler.GetType()} not supported");
    }
}
