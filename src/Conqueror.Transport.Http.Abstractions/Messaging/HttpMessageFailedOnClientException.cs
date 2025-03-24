using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;

// ReSharper disable once CheckNamespace
namespace Conqueror;

[Serializable]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "the standard constructors don't make sense for this class")]
public sealed class HttpMessageFailedOnClientException : MessageFailedException
{
    public HttpMessageFailedOnClientException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }

    private HttpMessageFailedOnClientException()
    {
    }

    public required HttpResponseMessage? Response { get; init; }

    public HttpStatusCode? StatusCode => Response?.StatusCode;
}
