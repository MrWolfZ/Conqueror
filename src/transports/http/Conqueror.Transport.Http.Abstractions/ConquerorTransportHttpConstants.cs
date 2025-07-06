using System.Diagnostics.CodeAnalysis;

namespace Conqueror;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "this API makes sense")]
public static class ConquerorTransportHttpConstants
{
    public const string TransportName = "http";

    public const string ServersSentEventsTransportName = "http-server-sent-events";

    public const string WebSocketsTransportName = "http-web-sockets";

    public static class MethodNames
    {
        public const string Delete = "DELETE";

        public const string Get = "GET";

        public const string Post = "POST";

        public const string Put = "PUT";

        public const string Patch = "PATCH";
    }

    public static class HeaderNames
    {
        public const string ContentType = "Content-Type";

        public const string ConquerorContext = "x-conqueror-context";

        public const string TraceParent = "traceparent";
    }

    public static class ContentTypes
    {
        public const string EventStream = "text/event-stream";

        public const string TextPlain = "text/plain";
    }

    public static class QueryParameterNames
    {
        public const string SignalSseEventType = "signalEventType";

        public const string SignalWebSocketsTag = "signalTag";

        public const string HeartbeatInterval = "heartbeatInterval";

        public const string HeartbeatTimeout = "hearbeatTimeout";
    }
}
