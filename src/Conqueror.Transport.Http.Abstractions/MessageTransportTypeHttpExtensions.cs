using System.ComponentModel;

namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class MessageTransportTypeHttpExtensions
{
    public static bool IsHttp(this MessageTransportType transportType) => transportType.Name == ConquerorTransportHttpConstants.TransportName;
}
