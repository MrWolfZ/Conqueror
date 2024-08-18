using System.Diagnostics;

namespace Conqueror.Streaming.Transport.Http.Client;

internal static class TracingHelper
{
    public static string CreateTraceParent(string traceVersion = "00",
                                           string? traceId = null,
                                           string? spanId = null,
                                           string traceFlags = "01")
    {
        traceId ??= ActivityTraceId.CreateRandom().ToString();
        spanId ??= ActivitySpanId.CreateRandom().ToString();

        return $"{traceVersion}-{traceId}-{spanId}-{traceFlags}";
    }
}
