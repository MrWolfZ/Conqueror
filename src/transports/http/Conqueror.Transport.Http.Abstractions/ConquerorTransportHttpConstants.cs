using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "this API makes sense")]
public static class ConquerorTransportHttpConstants
{
    public const string TransportName = "http";

    public const string ServersSentEventsTransportName = "http-server-sent-events";

    public const string ConquerorContextHeaderName = "x-conqueror-context";

    public const string TraceParentHeaderName = "traceparent";

    public const string MethodDelete = "DELETE";

    public const string MethodGet = "GET";

    public const string MethodPost = "POST";

    public const string MethodPut = "PUT";

    public const string MethodPatch = "PATCH";

    public static class HeaderNames
    {
        public const string ContentType = "Content-Type";
    }

    public static class ContentTypes
    {
        public const string EventStream = "text/event-stream";

        public const string TextPlain = "text/plain";
    }
}
