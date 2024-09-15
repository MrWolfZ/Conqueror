using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore;

internal static class HttpQueryExecutor
{
    public static Task<TResponse> Execute<TQuery, TResponse>(HttpContext httpContext, TQuery query, CancellationToken cancellationToken)
        where TQuery : class
    {
        var queryHandler = httpContext.RequestServices.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        return queryHandler.Handle(query, cancellationToken);
    }

    public static Task<TResponse> Execute<TQuery, TResponse>(HttpContext httpContext, CancellationToken cancellationToken)
        where TQuery : class, new()
    {
        var queryHandler = httpContext.RequestServices.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        return queryHandler.Handle(new(), cancellationToken);
    }
}
