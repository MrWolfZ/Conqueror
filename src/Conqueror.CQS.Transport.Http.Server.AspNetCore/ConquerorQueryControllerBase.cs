using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

// it makes sense for these types to be in the same file
#pragma warning disable SA1402

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    [ApiController]
    public abstract class ConquerorQueryControllerBase<TQuery, TResponse> : ConquerorCqsControllerBase
        where TQuery : class
    {
        protected async Task<TResponse> ExecuteQuery(TQuery query, CancellationToken cancellationToken)
        {
            return await ExecuteWithContext(async () =>
            {
                var queryHandler = Request.HttpContext.RequestServices.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
                return await queryHandler.ExecuteQuery(query, cancellationToken);
            });
        }
    }

    [ApiController]
    public abstract class ConquerorQueryWithoutPayloadControllerBase<TQuery, TResponse> : ConquerorCqsControllerBase
        where TQuery : class, new()
    {
        protected async Task<TResponse> ExecuteQuery(CancellationToken cancellationToken)
        {
            return await ExecuteWithContext(async () =>
            {
                var queryHandler = Request.HttpContext.RequestServices.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
                return await queryHandler.ExecuteQuery(new(), cancellationToken);
            });
        }
    }
}
