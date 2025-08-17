namespace Conqueror;

[SuppressMessage(
    "Roslynator",
    "RCS1194:Implement exception constructors",
    Justification = "the standard constructors don't make sense for this class"
)]
public sealed class HttpWebSocketsSignalFailedOnPublisherException : SignalFailedException
{
    public HttpWebSocketsSignalFailedOnPublisherException(string message, Exception innerException)
        : base(message, innerException) { }

    public HttpWebSocketsSignalFailedOnPublisherException(string message)
        : base(message) { }

    private HttpWebSocketsSignalFailedOnPublisherException() { }

    public override string WellKnownReason => WellKnownReasons.None;
}
