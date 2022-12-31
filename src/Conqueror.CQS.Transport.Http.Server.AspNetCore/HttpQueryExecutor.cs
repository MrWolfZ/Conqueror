using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    public static class HttpQueryExecutor
    {
        public static Task<TResponse> ExecuteQuery<TQuery, TResponse>(HttpContext httpContext, TQuery query, CancellationToken cancellationToken)
            where TQuery : class
        {
            return HttpRequestExecutor.ExecuteWithContext(httpContext, async () =>
            {
                var queryHandler = httpContext.RequestServices.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
                return await queryHandler.ExecuteQuery(query, cancellationToken);
            });
        }

        public static Task<TResponse> ExecuteQuery<TQuery, TResponse>(HttpContext httpContext, CancellationToken cancellationToken)
            where TQuery : class, new()
        {
            return HttpRequestExecutor.ExecuteWithContext(httpContext, async () =>
            {
                var queryHandler = httpContext.RequestServices.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
                return await queryHandler.ExecuteQuery(new(), cancellationToken);
            });
        }
    }
}
