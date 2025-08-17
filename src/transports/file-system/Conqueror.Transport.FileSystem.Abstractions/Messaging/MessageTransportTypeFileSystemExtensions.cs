namespace Conqueror;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class MessageTransportTypeFileSystemExtensions
{
    public static bool IsFileSystem(this MessageTransportType transportType) =>
        string.Equals(
            transportType.Name,
            ConquerorTransportFileSystemConstants.TransportName,
            StringComparison.Ordinal
        );
}
