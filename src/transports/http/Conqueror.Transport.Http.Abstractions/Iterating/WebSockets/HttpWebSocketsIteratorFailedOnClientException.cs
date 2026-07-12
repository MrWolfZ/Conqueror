namespace Conqueror;

[SuppressMessage(
    "Roslynator",
    "RCS1194:Implement exception constructors",
    Justification = "the standard constructors don't make sense for this class"
)]
public sealed class HttpWebSocketsIteratorFailedOnClientException : IteratorFailedException
{
    public HttpWebSocketsIteratorFailedOnClientException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public HttpWebSocketsIteratorFailedOnClientException(string message)
        : base(message)
    {
    }

    private HttpWebSocketsIteratorFailedOnClientException() { }

    public override string WellKnownReason => WellKnownReasons.None;
}
