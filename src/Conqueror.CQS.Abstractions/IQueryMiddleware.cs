using System.Threading.Tasks;

namespace Conqueror;

public interface IQueryMiddleware<TQuery, TResponse>
    where TQuery : class
{
    /// <summary>
    ///     Execute the middleware with a given context.
    /// </summary>
    /// <param name="ctx">The context for the query execution.</param>
    /// <returns>The response for the query execution</returns>
    Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx);
}
