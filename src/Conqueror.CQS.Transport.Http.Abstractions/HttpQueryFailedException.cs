using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;

namespace Conqueror;

[Serializable]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "the standard constructors don't make sense for this class")]
public sealed class HttpQueryFailedException : Exception
{
    public HttpQueryFailedException(string message, HttpResponseMessage? response, Exception? innerException = null)
        : base(message, innerException)
    {
        Response = response;
    }

    private HttpQueryFailedException()
    {
    }

    private HttpQueryFailedException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        : base(serializationInfo, streamingContext)
    {
    }

    public HttpResponseMessage? Response { get; }

    public HttpStatusCode? StatusCode => Response?.StatusCode;
}
