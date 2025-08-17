namespace Conqueror.Streaming.Transport.Http.Server.AspNetCore;

using System.Diagnostics;

public static class ConquerorServerTransportHelper
{
    public static void HandleTraceParent(ConquerorContext conquerorContext, string? traceParent)
    {
        if (Activity.Current is null && traceParent is not null)
        {
            using var a = new Activity("");
            var traceId = a.SetParentId(traceParent).TraceId.ToString();
            conquerorContext.TraceId = traceId;
        }
    }

    public static void SignalExecution(ConquerorContext conquerorContext, string transportTypeName) =>
        conquerorContext.SignalExecutionFromTransport(transportTypeName);
}
