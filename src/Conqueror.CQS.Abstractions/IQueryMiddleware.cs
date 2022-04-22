using System.Threading.Tasks;

// empty interface used as marker interface for other operations
#pragma warning disable CA1040

namespace Conqueror.CQS
{
    public interface IQueryMiddleware
    {
    }

    public interface IQueryMiddleware<TConfiguration> : IQueryMiddleware
        where TConfiguration : QueryMiddlewareConfigurationAttribute, IQueryMiddlewareConfiguration<IQueryMiddleware<TConfiguration>>
    {
        Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TConfiguration> ctx)
            where TQuery : class;
    }
}
