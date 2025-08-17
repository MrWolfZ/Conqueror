namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class MessageTransportTypeHttpExtensions
{
    public static bool IsHttp(this MessageTransportType transportType) =>
        string.Equals(transportType.Name, ConquerorTransportHttpConstants.TransportName, StringComparison.Ordinal);
}
