using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore;

internal sealed class CqsHttpTransportServerInvokerActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var invoker = context.HttpContext.RequestServices.GetRequiredService<CqsHttpTransportServerInvoker>();

        await invoker.Execute(() => next()).ConfigureAwait(false);
    }
}
