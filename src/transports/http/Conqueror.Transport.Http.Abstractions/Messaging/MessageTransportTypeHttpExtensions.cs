using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class MessageTransportTypeHttpExtensions
{
    public static bool IsHttp(this MessageTransportType transportType) => transportType.Name == ConquerorTransportHttpConstants.TransportName;
}
