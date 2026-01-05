namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class IteratorTransportTypeHttpWebSocketsExtensions
{
    public static bool IsHttpWebSockets(this IteratorTransportType transportType) =>
        string.Equals(
            transportType.Name,
            ConquerorTransportHttpConstants.WebSocketsTransportName,
            StringComparison.Ordinal
        );
}
