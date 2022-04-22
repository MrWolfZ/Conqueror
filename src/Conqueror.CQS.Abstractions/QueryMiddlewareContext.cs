using System.Threading;
using System.Threading.Tasks;

// these classes belong together
#pragma warning disable SA1402

namespace Conqueror.CQS
{
    public abstract class QueryMiddlewareContext<TQuery, TResponse>
        where TQuery : class
    {
        public abstract TQuery Query { get; }

        public abstract CancellationToken CancellationToken { get; }

        public abstract Task<TResponse> Next(TQuery query, CancellationToken cancellationToken);
    }

    public abstract class QueryMiddlewareContext<TQuery, TResponse, TConfiguration> : QueryMiddlewareContext<TQuery, TResponse>
        where TQuery : class
        where TConfiguration : QueryMiddlewareConfigurationAttribute
    {
        public abstract TConfiguration Configuration { get; }
    }
}
