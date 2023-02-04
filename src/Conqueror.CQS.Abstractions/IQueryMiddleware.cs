using System.Threading.Tasks;

namespace Conqueror
{
    public interface IQueryMiddleware
    {
        Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse> ctx)
            where TQuery : class;
    }

    public interface IQueryMiddleware<TConfiguration>
    {
        Task<TResponse> Execute<TQuery, TResponse>(QueryMiddlewareContext<TQuery, TResponse, TConfiguration> ctx)
            where TQuery : class;
    }
}
