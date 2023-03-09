using Conqueror.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore;

internal sealed class FormattedConquerorContextDataInvalidExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not FormattedConquerorContextDataInvalidException)
        {
            return;
        }

        // an invalid context could be a sign of malicious calls; therefore we don't
        // leak any details about what went wrong except that the context data was invalid
        context.Result = new BadRequestObjectResult("invalid conqueror context data");
    }
}
