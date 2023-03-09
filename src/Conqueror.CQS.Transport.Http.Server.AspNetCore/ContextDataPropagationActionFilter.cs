using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Conqueror.CQS.Transport.Http.Common;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore;

internal sealed class ContextDataPropagationActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        using var conquerorContext = context.HttpContext.RequestServices.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

        if (context.HttpContext.Request.Headers.TryGetValue(HttpConstants.ConquerorContextHeaderName, out var values))
        {
            try
            {
                // ReSharper disable once RedundantEnumerableCastCall (false positive)
                var parsedValue = ContextValueFormatter.Parse(values.AsEnumerable().OfType<string>());

                foreach (var (key, value) in parsedValue)
                {
                    conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
                }
            }
            catch
            {
                throw new BadContextException();
            }
        }

        conquerorContext.SignalExecutionFromTransport();

        if (Activity.Current is null && context.HttpContext.Request.Headers.TryGetValue(HeaderNames.TraceParent, out var traceParentValues) && traceParentValues.FirstOrDefault() is { } traceParent)
        {
            using var a = new Activity(string.Empty);
            var traceId = a.SetParentId(traceParent).TraceId.ToString();
            conquerorContext.SetTraceId(traceId);
        }

        _ = await next().ConfigureAwait(false);

        if (ContextValueFormatter.Format(conquerorContext.UpstreamContextData) is { } s)
        {
            context.HttpContext.Response.Headers.Add(HttpConstants.ConquerorContextHeaderName, s);
        }
    }
}
