namespace Conqueror;

/// <summary>
///     A signal transport that uses the file system to publish and receive signals.
/// </summary>
[SignalTransport(Prefix = "FileSystem", Namespace = "Conqueror")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class FileSystemSignalAttribute : Attribute
{
    /// <summary>
    ///     The tag to identify signals of this type when publishing via the file system.
    ///     Defaults to the dasherized type name of the signal type (stripped of the
    ///     <c>...Signal</c> suffix if any).
    /// </summary>
    public string? Tag { get; set; }
}
