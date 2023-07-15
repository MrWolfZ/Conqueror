using System.Collections.Generic;
using System.Linq;
using Conqueror.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Conqueror.CQS.Transport.Http.Server.AspNetCore;

internal sealed class CqsHttpTransportServerInvoker : ConquerorServerTransportInvoker
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public CqsHttpTransportServerInvoker(IConquerorContextAccessor conquerorContextAccessor, IHttpContextAccessor httpContextAccessor)
        : base(conquerorContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    protected override IEnumerable<string> GetFormattedDownstreamContextData()
    {
        if (httpContextAccessor.HttpContext?.Request.Headers.TryGetValue(HttpConstants.ConquerorDownstreamContextHeaderName, out var values) ?? false)
        {
            return values;
        }

        return Enumerable.Empty<string>();
    }

    protected override IEnumerable<string> GetFormattedContextData()
    {
        if (httpContextAccessor.HttpContext?.Request.Headers.TryGetValue(HttpConstants.ConquerorContextHeaderName, out var values) ?? false)
        {
            return values;
        }

        return Enumerable.Empty<string>();
    }

    protected override string? GetTraceParent()
    {
        if (httpContextAccessor.HttpContext?.Request.Headers.TryGetValue(HeaderNames.TraceParent, out var traceParentValues) ?? false)
        {
            return traceParentValues.FirstOrDefault();
        }

        return null;
    }

    protected override void SetFormattedUpstreamContextData(string formattedData)
    {
        httpContextAccessor.HttpContext?.Response.Headers.Add(HttpConstants.ConquerorUpstreamContextHeaderName, formattedData);
    }

    protected override void SetFormattedContextData(string formattedData)
    {
        httpContextAccessor.HttpContext?.Response.Headers.Add(HttpConstants.ConquerorContextHeaderName, formattedData);
    }
}
