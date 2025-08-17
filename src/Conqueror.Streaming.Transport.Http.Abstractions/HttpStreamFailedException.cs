namespace Conqueror;

using System.Diagnostics.CodeAnalysis;
using System.Net;

[SuppressMessage(
    "Roslynator",
    "RCS1194:Implement exception constructors",
    Justification = "the standard constructors don't make sense for this class"
)]
public sealed class HttpStreamFailedException : Exception
{
    public HttpStreamFailedException(string message, HttpStatusCode? statusCode, Exception? innerException = null)
        : base(message, innerException) => StatusCode = statusCode;

    private HttpStreamFailedException() { }

    public HttpStatusCode? StatusCode { get; }
}
