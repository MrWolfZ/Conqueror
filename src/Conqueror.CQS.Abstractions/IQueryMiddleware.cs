using System.Threading.Tasks;

namespace Conqueror.CQS
{
    public interface IQueryMiddleware<TConfiguration>
        where TConfiguration : QueryMiddlewareConfigurationAttribute, IQueryMiddlewareConfiguration<IQueryMiddleware<TConfiguration>>
    {
        Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TConfiguration> ctx)
            where TQuery : class;
    }
}
