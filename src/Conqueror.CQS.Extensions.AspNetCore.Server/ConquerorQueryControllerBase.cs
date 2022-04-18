using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

// it makes sense for these types to be in the same file
#pragma warning disable SA1402

namespace Conqueror.CQS.Extensions.AspNetCore.Server
{
    [ApiController]
    public abstract class ConquerorQueryControllerBase<TQuery, TResponse> : ControllerBase
        where TQuery : class
    {
        protected async Task<TResponse> ExecuteQuery(TQuery query, CancellationToken cancellationToken)
        {
            var queryHandler = Request.HttpContext.RequestServices.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
            return await queryHandler.ExecuteQuery(query, cancellationToken);
        }
    }

    [ApiController]
    public abstract class ConquerorQueryWithoutPayloadControllerBase<TQuery, TResponse> : ControllerBase
        where TQuery : class, new()
    {
        protected async Task<TResponse> ExecuteQuery(CancellationToken cancellationToken)
        {
            var queryHandler = Request.HttpContext.RequestServices.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
            return await queryHandler.ExecuteQuery(new(), cancellationToken);
        }
    }
}
