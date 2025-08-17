namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class SignalTransportTypeFileSystemExtensions
{
    public static bool IsFileSystem(this SignalTransportType transportType) =>
        string.Equals(
            transportType.Name,
            ConquerorTransportFileSystemConstants.TransportName,
            StringComparison.Ordinal
        );
}
