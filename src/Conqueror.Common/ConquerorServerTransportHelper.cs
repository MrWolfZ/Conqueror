using System.Diagnostics;

namespace Conqueror.Common;

public static class ConquerorServerTransportHelper
{
    public static void HandleTraceParent(IConquerorContext conquerorContext, string? traceParent)
    {
        if (Activity.Current is null && traceParent is not null)
        {
            using var a = new Activity(string.Empty);
            var traceId = a.SetParentId(traceParent).TraceId.ToString();
            conquerorContext.SetTraceId(traceId);
        }
    }

    public static void SignalExecution(IConquerorContext conquerorContext, string transportTypeName)
    {
        conquerorContext.SignalExecutionFromTransport(transportTypeName);
    }
}
