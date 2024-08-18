using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Conqueror;

[Serializable]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "the standard constructors don't make sense for this class")]
public sealed class HttpStreamFailedException : Exception
{
    public HttpStreamFailedException(string message, HttpStatusCode? statusCode, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    private HttpStreamFailedException()
    {
    }

    public HttpStatusCode? StatusCode { get; }
}
