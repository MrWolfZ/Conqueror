using System;
using System.Linq;
using System.Threading.Tasks;
using Conqueror.CQS.Transport.Http.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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

            var response = await executeFn();

            if (context.Items.Count > 0)
            {
                httpContext.Response.Headers.Add(HttpConstants.ConquerorContextHeaderName, ContextValueFormatter.Format(context.Items));
            }

            return response;
        }
    }
}
