using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class SignalTransportTypeFileSystemExtensions
{
    public static bool IsFileSystem(this SignalTransportType transportType)
        => transportType.Name == ConquerorTransportFileSystemConstants.TransportName;
}
