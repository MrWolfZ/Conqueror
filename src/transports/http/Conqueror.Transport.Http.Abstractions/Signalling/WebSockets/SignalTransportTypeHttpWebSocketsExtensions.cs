using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class SignalTransportTypeHttpWebSocketsExtensions
{
    public static bool IsHttpWebSockets(this SignalTransportType transportType)
        => transportType.Name == ConquerorTransportHttpConstants.WebSocketsTransportName;
}
