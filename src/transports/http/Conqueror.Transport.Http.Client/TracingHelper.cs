namespace Conqueror.Transport.Http.Client;

internal static class TracingHelper
{
    public static string CreateTraceParent(
        string traceVersion = "00",
        string? traceId = null,
        string? spanId = null,
        string traceFlags = "01"
    )
    {
        traceId ??= ActivityTraceId.CreateRandom().ToString();
        spanId ??= ActivitySpanId.CreateRandom().ToString();

        return $"{traceVersion}-{traceId}-{spanId}-{traceFlags}";
    }

    /// <summary>
    ///     During normal operations, when an activity as active, .NET will automatically send the 'traceparent'
    ///     header. However, the HTTP clients created by the ASP.NET Core test server do not propagate the this
    ///     header; to ensure that tracing works correctly during testing with Conqueror, we explicitly set the
    ///     'traceparent' header when we believe that we are running with a test client
    /// </summary>
    /// <param name="headers">The headers to which to add the traceparent header</param>
    /// <param name="httpClient">The http client for which to set the header</param>
    public static void SetTraceParentHeaderForTestClient(HttpHeaders headers, HttpClient httpClient)
    {
        if (
            Activity.Current?.Id is not { } id
            || !string.Equals(httpClient.BaseAddress?.AbsoluteUri, "http://conqueror.test/", StringComparison.Ordinal)
        )
        {
            return;
        }

        headers.Add(HeaderNames.TraceParent, id);
    }
}
