using System.ComponentModel;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class MessageTransportTypeHttpExtensions
{
    public static bool IsHttp(this MessageTransportType transportType) => transportType.Name == ConquerorTransportHttpConstants.TransportName;
}
