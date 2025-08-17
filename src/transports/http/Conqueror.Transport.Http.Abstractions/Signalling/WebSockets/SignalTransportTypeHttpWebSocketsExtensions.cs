namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class SignalTransportTypeHttpWebSocketsExtensions
{
    public static bool IsHttpWebSockets(this SignalTransportType transportType) =>
        string.Equals(
            transportType.Name,
            ConquerorTransportHttpConstants.WebSocketsTransportName,
            StringComparison.Ordinal
        );
}
