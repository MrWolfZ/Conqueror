using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class MessageTransportTypeFileSystemExtensions
{
    public static bool IsFileSystem(this MessageTransportType transportType) => transportType.Name == ConquerorTransportFileSystemConstants.TransportName;
}
