using System.Collections.Generic;
using System.Diagnostics;

namespace Conqueror.Common;

public static class ConquerorServerTransportHelper
{
    public static void ReadContextData(IConquerorContext conquerorContext, IEnumerable<string> formattedDownstreamContextData, IEnumerable<string> formattedContextData)
    {
        var parsedDownstreamData = ConquerorContextDataFormatter.Parse(formattedDownstreamContextData);

        foreach (var (key, value) in parsedDownstreamData)
        {
            conquerorContext.DownstreamContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
        }

        var parsedData = ConquerorContextDataFormatter.Parse(formattedContextData);

        foreach (var (key, value) in parsedData)
        {
            conquerorContext.ContextData.Set(key, value, ConquerorContextDataScope.AcrossTransports);
        }
    }

    public static void HandleTraceParent(IConquerorContext conquerorContext, string? traceParent)
    {
        if (Activity.Current is null && traceParent is not null)
        {
            using var a = new Activity(string.Empty);
            var traceId = a.SetParentId(traceParent).TraceId.ToString();
            conquerorContext.SetTraceId(traceId);
        }
    }

    public static void SignalExecution(IConquerorContext conquerorContext)
    {
        conquerorContext.SignalExecutionFromTransport();
    }
}
