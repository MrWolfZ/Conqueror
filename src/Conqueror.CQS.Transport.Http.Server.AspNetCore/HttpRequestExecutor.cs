using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Conqueror.CQS.Transport.Http.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore
{
    internal static class HttpRequestExecutor
    {
        public static async Task<TResponse> ExecuteWithContext<TResponse>(HttpContext httpContext, Func<Task<TResponse>> executeFn)
        {
            using var context = httpContext.RequestServices.GetRequiredService<IConquerorContextAccessor>().GetOrCreate();

            if (httpContext.Request.Headers.TryGetValue(HttpConstants.ConquerorContextHeaderName, out var values))
            {
                try
                {
                    // ReSharper disable once RedundantEnumerableCastCall (false positive)
                    var parsedValue = ContextValueFormatter.Parse(values.AsEnumerable().OfType<string>());

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

            if (httpContext.Request.Headers.TryGetValue(HttpConstants.ConquerorCommandIdHeaderName, out var commandIdValues) && commandIdValues.FirstOrDefault() is { } commandId)
            {
                httpContext.RequestServices.GetRequiredService<ICommandContextAccessor>().SetExternalCommandId(commandId);
            }

            if (httpContext.Request.Headers.TryGetValue(HttpConstants.ConquerorQueryIdHeaderName, out var queryIdValues) && queryIdValues.FirstOrDefault() is { } queryId)
            {
                httpContext.RequestServices.GetRequiredService<IQueryContextAccessor>().SetExternalQueryId(queryId);
            }

            if (Activity.Current is null)
            {
                if (httpContext.Request.Headers.TryGetValue(HeaderNames.TraceParent, out var traceParentValues) && traceParentValues.FirstOrDefault() is { } traceParent)
                {
                    using var a = new Activity(string.Empty);
                    var traceId = a.SetParentId(traceParent).TraceId.ToString();
                    context.SetTraceId(traceId);
                }
                else if (httpContext.Request.Headers.TryGetValue(HttpConstants.ConquerorTraceIdHeaderName, out var traceIdValues) && traceIdValues.FirstOrDefault() is { } traceId)
                {
                    context.SetTraceId(traceId);
                }
            }

            var response = await executeFn().ConfigureAwait(false);

            if (context.HasItems)
            {
                httpContext.Response.Headers.Add(HttpConstants.ConquerorContextHeaderName, ContextValueFormatter.Format(context.Items));
            }

            return response;
        }
    }
}
