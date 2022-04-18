using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryHandlerProxy<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
        where TQuery : class
    {
        private readonly QueryMiddlewaresInvoker invoker;
        private readonly QueryHandlerRegistry registry;
        private readonly IServiceProvider serviceProvider;

        public QueryHandlerProxy(QueryHandlerRegistry registry, QueryMiddlewaresInvoker invoker, IServiceProvider serviceProvider)
        {
            this.registry = registry;
            this.invoker = invoker;
            this.serviceProvider = serviceProvider;
        }

        public Task<TResponse> ExecuteQuery(TQuery query, CancellationToken cancellationToken)
        {
            var (queryHandler, metadata) = registry.GetQueryHandler<TQuery, TResponse>(serviceProvider);
            return invoker.InvokeMiddlewares(serviceProvider, queryHandler, metadata, query, cancellationToken);
        }
    }
}
