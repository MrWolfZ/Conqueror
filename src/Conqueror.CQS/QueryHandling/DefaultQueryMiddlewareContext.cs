using System.Threading;
using System.Threading.Tasks;

// these classes belong together
#pragma warning disable SA1402

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class DefaultQueryMiddlewareContext<TQuery, TResponse> : QueryMiddlewareContext<TQuery, TResponse>
        where TQuery : class
    {
        private readonly QueryMiddlewareNext<TQuery, TResponse> next;

        public DefaultQueryMiddlewareContext(TQuery query, QueryMiddlewareNext<TQuery, TResponse> next, CancellationToken cancellationToken)
        {
            this.next = next;
            Query = query;
            CancellationToken = cancellationToken;
        }

        public override TQuery Query { get; }

        public override CancellationToken CancellationToken { get; }

        public override Task<TResponse> Next(TQuery query, CancellationToken cancellationToken) => next(query, cancellationToken);
    }
    
    internal sealed class DefaultQueryMiddlewareContext<TQuery, TResponse, TConfiguration> : QueryMiddlewareContext<TQuery, TResponse, TConfiguration>
        where TQuery : class
    {
        private readonly QueryMiddlewareNext<TQuery, TResponse> next;

        public DefaultQueryMiddlewareContext(TQuery query, QueryMiddlewareNext<TQuery, TResponse> next, TConfiguration configuration, CancellationToken cancellationToken)
        {
            this.next = next;
            Query = query;
            CancellationToken = cancellationToken;
            Configuration = configuration;
        }

        public override TQuery Query { get; }

        public override CancellationToken CancellationToken { get; }

        public override TConfiguration Configuration { get; }

        public override Task<TResponse> Next(TQuery query, CancellationToken cancellationToken) => next(query, cancellationToken);
    }
}
