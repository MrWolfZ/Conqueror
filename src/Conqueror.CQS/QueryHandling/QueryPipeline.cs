﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryPipeline
    {
        private readonly ConquerorContextAccessor conquerorContextAccessor;
        private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, IQueryMiddlewareInvoker Invoker)> middlewares;
        private readonly QueryContextAccessor queryContextAccessor;

        public QueryPipeline(QueryContextAccessor queryContextAccessor,
                             ConquerorContextAccessor conquerorContextAccessor,
                             List<(Type MiddlewareType, object? MiddlewareConfiguration, IQueryMiddlewareInvoker Invoker)> middlewares)
        {
            this.queryContextAccessor = queryContextAccessor;
            this.conquerorContextAccessor = conquerorContextAccessor;
            this.middlewares = middlewares;
        }

        public async Task<TResponse> Execute<TQuery, TResponse>(IServiceProvider serviceProvider,
                                                                TQuery initialQuery,
                                                                IQueryTransportClient transportClient,
                                                                CancellationToken cancellationToken)
            where TQuery : class
        {
            var queryContext = new DefaultQueryContext(initialQuery);

            queryContextAccessor.QueryContext = queryContext;

            using var conquerorContext = conquerorContextAccessor.GetOrCreate();

            var finalResponse = await ExecuteNextMiddleware(0, initialQuery, cancellationToken);

            queryContextAccessor.ClearContext();

            return finalResponse;

            async Task<TResponse> ExecuteNextMiddleware(int index, TQuery query, CancellationToken token)
            {
                queryContext.SetQuery(query);

                if (index >= middlewares.Count)
                {
                    var responseFromHandler = await transportClient.ExecuteQuery<TQuery, TResponse>(query, token);
                    queryContext.SetResponse(responseFromHandler!);
                    return responseFromHandler;
                }

                var (_, middlewareConfiguration, invoker) = middlewares[index];
                var response = await invoker.Invoke(query, (q, t) => ExecuteNextMiddleware(index + 1, q, t), middlewareConfiguration, serviceProvider, token);
                queryContext.SetResponse(response!);
                return response;
            }
        }
    }
}
