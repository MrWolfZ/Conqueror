using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.Extensions.AspNetCore.Server
{
    [TypeFilter(typeof(ExceptionHandlerFilter))]
    public abstract class ConquerorControllerBase : ControllerBase
    {
        protected async Task<TResponse> ExecuteWithContext<TResponse>(Func<Task<TResponse>> executeFn)
        {
            using var context = Request.HttpContext.RequestServices.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

            if (Request.Headers.TryGetValue(HttpConstants.ConquerorContextHeaderName, out var values))
            {
                try
                {
                    var parsedValue = ContextValueFormatter.Parse(values.AsEnumerable());

                    foreach (var (key, value) in parsedValue)
                    {
                        context.Items[key] = value;
                    }
                }
                catch
                {
                    throw new BadContextException();
                }
            }

            var response = await executeFn();

            if (context.Items.Count > 0)
            {
                Response.Headers.Add(HttpConstants.ConquerorContextHeaderName, ContextValueFormatter.Format(context.Items));
            }

            return response;
        }

        private sealed class ExceptionHandlerFilter : IExceptionFilter
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

#pragma warning disable
        private sealed class BadContextException : Exception
#pragma warning restore
        {
        }
    }
}
