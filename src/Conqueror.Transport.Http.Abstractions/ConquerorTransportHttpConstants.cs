using System.ComponentModel;

namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConquerorTransportHttpConstants
{
    public const string TransportName = "http";

    public const string ConquerorContextHeaderName = "x-conqueror-context";

    public const string TraceParentHeaderName = "traceparent";

    public const string MethodDelete = "DELETE";

    public const string MethodGet = "GET";

    public const string MethodPost = "POST";
}
