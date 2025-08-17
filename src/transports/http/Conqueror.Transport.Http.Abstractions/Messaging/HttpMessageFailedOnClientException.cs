namespace Conqueror;

[SuppressMessage(
    "Roslynator",
    "RCS1194:Implement exception constructors",
    Justification = "the standard constructors don't make sense for this class"
)]
public sealed class HttpMessageFailedOnClientException : MessageFailedException
{
    public HttpMessageFailedOnClientException(string message, Exception innerException)
        : base(message, innerException) { }

    public HttpMessageFailedOnClientException(string message)
        : base(message) { }

    private HttpMessageFailedOnClientException() { }

    public required HttpResponseMessage? Response { get; init; }

    public HttpStatusCode? StatusCode => Response?.StatusCode;

    public override string WellKnownReason => WellKnownReasons.None;
}
