namespace Conqueror;

[SuppressMessage(
    "Roslynator",
    "RCS1194:Implement exception constructors",
    Justification = "the standard constructors don't make sense for this class"
)]
public sealed class FileSystemSignalFailedOnPublisherException : SignalFailedException
{
    public FileSystemSignalFailedOnPublisherException(string message, Exception innerException)
        : base(message, innerException) { }

    public FileSystemSignalFailedOnPublisherException(string message)
        : base(message) { }

    private FileSystemSignalFailedOnPublisherException() { }

    public override string WellKnownReason => WellKnownReasons.None;
}
