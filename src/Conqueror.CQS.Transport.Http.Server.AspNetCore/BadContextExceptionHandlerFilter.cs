using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    internal sealed class BadContextExceptionHandlerFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is not BadContextException)
            {
                return;
            }

            context.Result = new BadRequestObjectResult("invalid command context header");
        }
    }
}
