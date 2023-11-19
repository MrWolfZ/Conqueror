using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Conqueror.Eventing.Transport.WebSockets.Client;

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

    /// <summary>
    ///     The HTTP clients created by the ASP.NET Core test server do not propagate the traceparent header;
    ///     To ensure that tracing works correctly during testing with Conqueror, we explicitly set the
    ///     traceparent header when we believe that we are running with a test client.
    /// </summary>
    public static void SetTraceParentHeaderForTestClient(HttpHeaders headers, HttpClient httpClient)
    {
        // we use the default base address for the test client to detect it; this isn't perfect, but is
        // the most pragmatic solution that doesn't cost performance and has the lowest risk of interference
        // with normal operations
        if (Activity.Current?.Id is not { } id || httpClient.BaseAddress?.AbsoluteUri != "http://localhost/")
        {
            return;
        }

        headers.Add(HttpConstants.TraceParentHeaderName, id);
    }
}
