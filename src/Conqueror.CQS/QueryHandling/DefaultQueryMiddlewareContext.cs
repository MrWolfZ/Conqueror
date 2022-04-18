using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class DefaultQueryMiddlewareContext<TQuery, TResponse, TConfiguration> : QueryMiddlewareContext<TQuery, TResponse, TConfiguration>
        where TQuery : class
        where TConfiguration : QueryMiddlewareConfigurationAttribute, IQueryMiddlewareConfiguration<IQueryMiddleware<TConfiguration>>
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
