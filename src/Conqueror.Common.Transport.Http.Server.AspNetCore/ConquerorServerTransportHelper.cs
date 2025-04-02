using System.Diagnostics;

namespace Conqueror.Common.Transport.Http.Server.AspNetCore;

public static class ConquerorServerTransportHelper
{
    public static void HandleTraceParent(ConquerorContext conquerorContext, string? traceParent)
    {
        if (Activity.Current is null && traceParent is not null)
        {
            using var a = new Activity(string.Empty);
            var traceId = a.SetParentId(traceParent).TraceId.ToString();
            conquerorContext.SetTraceId(traceId);
        }
    }

    public static void SignalExecution(ConquerorContext conquerorContext, string transportTypeName)
    {
        conquerorContext.SignalExecutionFromTransport(transportTypeName);
    }
}
