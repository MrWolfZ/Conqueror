namespace Conqueror;

[SuppressMessage(
    "Roslynator",
    "RCS1194:Implement exception constructors",
    Justification = "the standard constructors don't make sense for this class"
)]
public sealed class FileSystemMessageFailedOnSenderException : MessageFailedException
{
    public FileSystemMessageFailedOnSenderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public FileSystemMessageFailedOnSenderException(string message)
        : base(message)
    {
    }

    private FileSystemMessageFailedOnSenderException() { }

    public override string WellKnownReason => WellKnownReasons.None;
}
